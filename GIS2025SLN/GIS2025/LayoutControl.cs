using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using XGIS;

namespace GIS2025
{
    public enum LayoutTool
    {
        None, PanPaper, Select,
        CreateMapFrame,
        ResizeElement, PanMapContent,
        CreateNorthArrow,
        CreateScaleBar,
        ToggleGrid
    }

    public enum ResizeHandle { None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }

    public partial class LayoutControl : UserControl
    {
        private XLayoutPage page;
        private float zoomScale = 1.0f;
        private float offsetX = 40;
        private float offsetY = 40;

        public LayoutTool CurrentTool = LayoutTool.Select;
        private XLayoutElement selectedElement = null;
        private XMapFrame activeMapFrame = null;

        private Point mouseDownLoc;
        private RectangleF originalBounds;
        private ResizeHandle currentHandle = ResizeHandle.None;

        private List<XVectorLayer> _cacheLayers;
        private XTileLayer _cacheBaseLayer;
        private XExtent _cacheExtent;

        private NorthArrowStyle _pendingNorthArrowStyle;
        private ScaleBarStyle _pendingScaleBarStyle;

        private ContextMenuStrip contextMenuLayout;

        public LayoutControl()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            InitContextMenu();

            // 绑定事件
            layoutBox.MouseWheel += LayoutBox_MouseWheel;
            layoutBox.MouseDown += LayoutBox_MouseDown;
            layoutBox.MouseMove += LayoutBox_MouseMove;
            layoutBox.MouseUp += LayoutBox_MouseUp;
            layoutBox.Paint += LayoutBox_Paint;
        }

        // ==========================================
        // 1. 初始化与菜单
        // ==========================================
        private void InitContextMenu()
        {
            contextMenuLayout = new ContextMenuStrip();
            var btnActivate = new ToolStripMenuItem("激活地图框");
            btnActivate.Click += (s, e) => { ToggleMapActivation(true); };

            var btnCloseActivate = new ToolStripMenuItem("关闭激活");
            btnCloseActivate.Click += (s, e) => { ToggleMapActivation(false); };

            contextMenuLayout.Items.Add(btnActivate);
            contextMenuLayout.Items.Add(btnCloseActivate);

            // 动态显示逻辑：只有选中地图框时才显示激活选项
            contextMenuLayout.Opening += (s, e) =>
            {
                if (selectedElement is XMapFrame)
                {
                    bool isActive = (activeMapFrame == selectedElement);
                    btnActivate.Visible = !isActive;
                    btnCloseActivate.Visible = isActive;
                }
                else
                {
                    e.Cancel = true; // 不是地图框就不显示
                }
            };
        }

        public void UpdateLayout(List<XVectorLayer> layers, XTileLayer baseLayer, XExtent currentExtent)
        {
            _cacheLayers = layers;
            _cacheBaseLayer = baseLayer;
            _cacheExtent = currentExtent;

            if (page == null) page = new XLayoutPage();
            else
            {
                // 更新现有 MapFrame 的引用，保证所见即所得
                foreach (var ele in page.Elements)
                {
                    if (ele is XMapFrame mapFrame)
                    {
                        mapFrame.Layers = layers;
                        mapFrame.BaseLayer = baseLayer;
                    }
                }
            }
            layoutBox.Invalidate();
        }

        // ==========================================
        // 2. 工具启动方法
        // ==========================================
        public void StartCreateMapFrame()
        {
            CurrentTool = LayoutTool.CreateMapFrame;
            layoutBox.Cursor = Cursors.Cross;
            DeselectAll();
        }

        public void StartCreateNorthArrow(NorthArrowStyle style)
        {
            CurrentTool = LayoutTool.CreateNorthArrow;
            _pendingNorthArrowStyle = style;
            layoutBox.Cursor = Cursors.Cross;
            DeselectAll();
        }

        public void StartCreateScaleBar(ScaleBarStyle style)
        {
            CurrentTool = LayoutTool.CreateScaleBar;
            _pendingScaleBarStyle = style;
            layoutBox.Cursor = Cursors.Cross;
            DeselectAll();
        }

        public void StartToggleGrid()
        {
            CurrentTool = LayoutTool.ToggleGrid;
            layoutBox.Cursor = Cursors.Hand;
            DeselectAll();
        }

        private void DeselectAll()
        {
            selectedElement = null;
            activeMapFrame = null;
            layoutBox.Invalidate();
        }

        private void ToggleMapActivation(bool activate)
        {
            if (activate && selectedElement is XMapFrame mapFrame)
            {
                activeMapFrame = mapFrame;
                CurrentTool = LayoutTool.PanMapContent;
                layoutBox.Cursor = Cursors.NoMove2D;
            }
            else
            {
                activeMapFrame = null;
                CurrentTool = LayoutTool.Select;
                layoutBox.Cursor = Cursors.Default;
            }
            layoutBox.Invalidate();
        }

        // ==========================================
        // 3. 鼠标交互逻辑
        // ==========================================
        private void LayoutBox_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDownLoc = e.Location;
            // 获取准确 DPI，这点非常重要，否则选不中！
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) { dpi = g.DpiX; }
            float pixelPerMM = dpi / 25.4f;

            // 1. 激活状态下：屏蔽大部分布局操作，只响应右键菜单
            if (activeMapFrame != null)
            {
                if (e.Button == MouseButtons.Right) contextMenuLayout.Show(layoutBox, e.Location);
                return;
            }

            // 2. 创建或特殊工具状态：不触发选择
            if (CurrentTool == LayoutTool.CreateMapFrame ||
                CurrentTool == LayoutTool.CreateNorthArrow ||
                CurrentTool == LayoutTool.CreateScaleBar ||
                CurrentTool == LayoutTool.ToggleGrid)
            {
                return;
            }

            // 3. 右键点击：尝试选中元素并弹出菜单
            if (e.Button == MouseButtons.Right)
            {
                if (CheckHitElement(e.Location, pixelPerMM))
                {
                    contextMenuLayout.Show(layoutBox, e.Location);
                }
                return;
            }

            // 4. 左键点击
            if (e.Button == MouseButtons.Left)
            {
                // A. 检查是否点中控制点（缩放）
                if (selectedElement != null && selectedElement.IsSelected)
                {
                    currentHandle = CheckHitHandle(e.Location, MMToPixelRect(selectedElement.Bounds, dpi));
                    if (currentHandle != ResizeHandle.None)
                    {
                        CurrentTool = LayoutTool.ResizeElement;
                        originalBounds = selectedElement.Bounds;
                        return;
                    }
                }

                // B. 检查是否点中元素（选择/移动）
                if (CheckHitElement(e.Location, pixelPerMM))
                {
                    CurrentTool = LayoutTool.Select;
                    originalBounds = selectedElement.Bounds;
                }
                else
                {
                    // C. 点空了 -> 平移纸张
                    CurrentTool = LayoutTool.PanPaper;
                    selectedElement = null;
                    if (page != null) foreach (var ele in page.Elements) ele.IsSelected = false;
                    layoutBox.Invalidate();
                }
            }
            // 5. 中键平移
            else if (e.Button == MouseButtons.Middle)
            {
                CurrentTool = LayoutTool.PanPaper;
            }
        }

        private void LayoutBox_MouseMove(object sender, MouseEventArgs e)
        {
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) { dpi = g.DpiX; }
            float pixelPerMM = dpi / 25.4f;

            // 1. 激活状态：漫游地图内容
            if (activeMapFrame != null)
            {
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
                {
                    XVertex v1 = activeMapFrame.FrameView.ToMapVertex(mouseDownLoc);
                    XVertex v2 = activeMapFrame.FrameView.ToMapVertex(e.Location);
                    activeMapFrame.FrameView.OffsetCenter(v1, v2);
                    mouseDownLoc = e.Location;
                    layoutBox.Invalidate();
                }
                return;
            }

            // 2. 创建状态：刷新橡皮筋框
            if ((CurrentTool == LayoutTool.CreateMapFrame ||
                 CurrentTool == LayoutTool.CreateNorthArrow ||
                 CurrentTool == LayoutTool.CreateScaleBar) && e.Button == MouseButtons.Left)
            {
                layoutBox.Invalidate();
                return;
            }

            // 3. 调整大小
            if (CurrentTool == LayoutTool.ResizeElement && selectedElement != null)
            {
                float dx = (e.X - mouseDownLoc.X) / zoomScale / pixelPerMM;
                float dy = (e.Y - mouseDownLoc.Y) / zoomScale / pixelPerMM;
                RectangleF newBounds = originalBounds;
                switch (currentHandle)
                {
                    case ResizeHandle.Right: newBounds.Width += dx; break;
                    case ResizeHandle.Bottom: newBounds.Height += dy; break;
                    case ResizeHandle.BottomRight: newBounds.Width += dx; newBounds.Height += dy; break;
                    case ResizeHandle.TopLeft: newBounds.X += dx; newBounds.Y += dy; newBounds.Width -= dx; newBounds.Height -= dy; break;
                }
                if (newBounds.Width < 5) newBounds.Width = 5;
                if (newBounds.Height < 5) newBounds.Height = 5;
                selectedElement.Bounds = newBounds;
                layoutBox.Invalidate();
            }
            // 4. 移动元素
            else if (CurrentTool == LayoutTool.Select && e.Button == MouseButtons.Left && selectedElement != null)
            {
                float dx = (e.X - mouseDownLoc.X) / zoomScale / pixelPerMM;
                float dy = (e.Y - mouseDownLoc.Y) / zoomScale / pixelPerMM;
                selectedElement.Bounds = new RectangleF(originalBounds.X + dx, originalBounds.Y + dy, originalBounds.Width, originalBounds.Height);
                layoutBox.Invalidate();
            }
            // 5. 平移纸张
            else if (CurrentTool == LayoutTool.PanPaper && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle))
            {
                offsetX += e.X - mouseDownLoc.X;
                offsetY += e.Y - mouseDownLoc.Y;
                mouseDownLoc = e.Location;
                layoutBox.Invalidate();
            }

            UpdateCursor(e.Location, dpi);
        }

        private void LayoutBox_MouseUp(object sender, MouseEventArgs e)
        {
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) dpi = g.DpiX;

            Action<XLayoutElement> FinishCreation = (newEle) => {
                page.Elements.Add(newEle);
                selectedElement = newEle;
                selectedElement.IsSelected = true;
                CurrentTool = LayoutTool.Select;
                layoutBox.Cursor = Cursors.Default;
                layoutBox.Invalidate();
            };

            Rectangle screenRect = GetScreenRectFromPoints(mouseDownLoc, e.Location);

            // 1. 创建 MapFrame
            if (CurrentTool == LayoutTool.CreateMapFrame && e.Button == MouseButtons.Left)
            {
                if (screenRect.Width > 10 && screenRect.Height > 10)
                {
                    RectangleF mmRect = PixelToMMRect(screenRect, dpi);
                    if (_cacheLayers != null)
                    {
                        XExtent newExtent = _cacheExtent != null ? new XExtent(_cacheExtent) : new XExtent(0, 10, 0, 10);
                        FinishCreation(new XMapFrame(mmRect, _cacheLayers, _cacheBaseLayer, newExtent));
                        return;
                    }
                }
                CurrentTool = LayoutTool.Select;
                layoutBox.Invalidate();
            }
            // 2. 创建指北针
            else if (CurrentTool == LayoutTool.CreateNorthArrow && e.Button == MouseButtons.Left)
            {
                RectangleF mmRect;
                if (screenRect.Width < 5)
                {
                    RectangleF clickPt = PixelToMMRect(new Rectangle(e.X, e.Y, 0, 0), dpi);
                    mmRect = new RectangleF(clickPt.X, clickPt.Y, 20, 20);
                }
                else mmRect = PixelToMMRect(screenRect, dpi);
                FinishCreation(new XNorthArrow(mmRect, _pendingNorthArrowStyle));
                return;
            }
            // 3. 创建比例尺
            else if (CurrentTool == LayoutTool.CreateScaleBar && e.Button == MouseButtons.Left)
            {
                // 【核心修改】寻找一个地图框来绑定
                XMapFrame targetFrame = null;

                // 1. 如果当前有激活的，就用激活的
                if (activeMapFrame != null) targetFrame = activeMapFrame;
                // 2. 如果没有，就找页面里第一个 MapFrame
                else
                {
                    foreach (var ele in page.Elements)
                    {
                        if (ele is XMapFrame mf) { targetFrame = mf; break; }
                    }
                }

                if (targetFrame == null)
                {
                    MessageBox.Show("请先创建一个地图框，才能添加比例尺！");
                    CurrentTool = LayoutTool.Select;
                    layoutBox.Cursor = Cursors.Default;
                    layoutBox.Invalidate();
                    return;
                }

                RectangleF mmRect;
                if (screenRect.Width < 5)
                {
                    RectangleF clickPt = PixelToMMRect(new Rectangle(e.X, e.Y, 0, 0), dpi);
                    mmRect = new RectangleF(clickPt.X, clickPt.Y, 40, 10);
                }
                else mmRect = PixelToMMRect(screenRect, dpi);

                // 传入 targetFrame
                FinishCreation(new XScaleBar(mmRect, _pendingScaleBarStyle, targetFrame));
                return;
            }
            // 4. 经纬网开关 (修复版：穿透查找，专门找地图框)
            else if (CurrentTool == LayoutTool.ToggleGrid && e.Button == MouseButtons.Left)
            {
                float pixelPerMM = dpi / 25.4f;
                bool found = false;

                // 倒序遍历，查找鼠标下的所有元素
                for (int i = page.Elements.Count - 1; i >= 0; i--)
                {
                    var ele = page.Elements[i];

                    // 【关键修复】这里只关心是不是 XMapFrame
                    if (ele is XMapFrame mapFrame)
                    {
                        // 计算该地图框在屏幕上的位置
                        RectangleF rect = MMToPixelRect(ele.Bounds, dpi);

                        // 稍微扩大一点点击范围 (容错 2px)
                        rect.Inflate(2, 2);

                        if (rect.Contains(e.Location))
                        {
                            // 找到了！切换状态
                            mapFrame.ShowGrid = !mapFrame.ShowGrid;
                            found = true;
                            break; // 找到一个就停，避免一次点穿多个
                        }
                    }
                }

                if (found)
                {
                    // 如果点中了，就切回普通选择模式
                    CurrentTool = LayoutTool.Select;
                    layoutBox.Cursor = Cursors.Default;
                    layoutBox.Invalidate(); // 重绘以显示/隐藏网格
                }
                else
                {
                    // 如果没点中，给个提示（可选）
                    // MessageBox.Show("请点击地图框区域");
                }
                return;
            }

            // 重置状态
            currentHandle = ResizeHandle.None;
            if (activeMapFrame == null && CurrentTool != LayoutTool.Select)
            {
                if (CurrentTool == LayoutTool.PanPaper || CurrentTool == LayoutTool.ResizeElement)
                    CurrentTool = LayoutTool.Select;
            }
            layoutBox.Invalidate();
        }

        private void LayoutBox_Paint(object sender, PaintEventArgs e)
        {
            if (page == null) return;
            float dpi = e.Graphics.DpiX;

            // 画纸张和元素
            page.Draw(e.Graphics, dpi, zoomScale, offsetX, offsetY);

            // 画选中状态
            if (activeMapFrame == null && selectedElement != null && selectedElement.IsSelected)
                DrawSelectionHandles(e.Graphics, dpi);

            // 画激活状态 (红框)
            if (activeMapFrame != null)
            {
                RectangleF rect = MMToPixelRect(activeMapFrame.Bounds, dpi);
                using (Pen p = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash })
                    e.Graphics.DrawRectangle(p, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
            }

            // 画创建时的蓝框
            bool isCreating = CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow || CurrentTool == LayoutTool.CreateScaleBar;
            if (isCreating && mouseDownLoc != Point.Empty && Control.MouseButtons == MouseButtons.Left)
            {
                Point currentMouse = layoutBox.PointToClient(Cursor.Position);
                Rectangle rect = GetScreenRectFromPoints(mouseDownLoc, currentMouse);
                using (Pen p = new Pen(Color.Blue, 1) { DashStyle = DashStyle.Dot })
                    e.Graphics.DrawRectangle(p, rect);
            }

            // 画标尺 (已修复)
            DrawRulers(e.Graphics, dpi, zoomScale, offsetX, offsetY);
        }

        // ==========================================
        // 4. 辅助绘制与计算
        // ==========================================
        private void LayoutBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (activeMapFrame != null)
            {
                bool isZoomIn = e.Delta > 0;
                XVertex center = activeMapFrame.FrameView.ToMapVertex(e.Location);
                activeMapFrame.FrameView.CurrentMapExtent.ZoomToCenter(center, isZoomIn ? 1.1 : 0.9);
                layoutBox.Invalidate();
            }
            else
            {
                if (e.Delta > 0) zoomScale *= 1.1f; else zoomScale /= 1.1f;
                layoutBox.Invalidate();
            }
        }

        // 【修复】完整的标尺绘制逻辑
        private void DrawRulers(Graphics g, float dpi, float zoom, float offX, float offY)
        {
            float pixelPerMM = dpi / 25.4f;
            int rulerThickness = 25;
            Pen linePen = Pens.Black;
            Font rulerFont = new Font("Arial", 7);
            Brush textBrush = Brushes.Black;

            // 背景
            g.FillRectangle(Brushes.WhiteSmoke, rulerThickness, 0, layoutBox.Width, rulerThickness);
            g.FillRectangle(Brushes.WhiteSmoke, 0, rulerThickness, rulerThickness, layoutBox.Height);
            g.FillRectangle(Brushes.LightGray, 0, 0, rulerThickness, rulerThickness);
            g.DrawLine(Pens.Gray, 0, rulerThickness, layoutBox.Width, rulerThickness);
            g.DrawLine(Pens.Gray, rulerThickness, 0, rulerThickness, layoutBox.Height);

            // 横向刻度
            for (int i = 0; i <= 500; i += 1) // 500mm 范围
            {
                float sx = offX + (i * pixelPerMM * zoom);
                if (sx > rulerThickness && sx < layoutBox.Width)
                {
                    if (i % 10 == 0) // 大刻度 (1cm)
                    {
                        g.DrawLine(linePen, sx, 0, sx, 15);
                        string t = i.ToString();
                        g.DrawString(t, rulerFont, textBrush, sx - g.MeasureString(t, rulerFont).Width / 2, 12);
                    }
                    else if (i % 5 == 0 && zoom > 0.5) // 中刻度 (0.5cm)
                    {
                        g.DrawLine(linePen, sx, 10, sx, 15);
                    }
                }
            }

            // 纵向刻度
            for (int i = 0; i <= 500; i += 1)
            {
                float sy = offY + (i * pixelPerMM * zoom);
                if (sy > rulerThickness && sy < layoutBox.Height)
                {
                    if (i % 10 == 0)
                    {
                        g.DrawLine(linePen, 0, sy, 15, sy);
                        g.DrawString(i.ToString(), rulerFont, textBrush, 2, sy - 6);
                    }
                    else if (i % 5 == 0 && zoom > 0.5)
                    {
                        g.DrawLine(linePen, 10, sy, 15, sy);
                    }
                }
            }
        }

        private void DrawSelectionHandles(Graphics g, float dpi)
        {
            RectangleF rect = MMToPixelRect(selectedElement.Bounds, dpi);
            int s = 6;
            // 绘制 8 个控制点
            PointF[] pts = GetHandlePoints(rect);
            foreach (PointF p in pts)
            {
                g.FillRectangle(Brushes.White, p.X - s / 2, p.Y - s / 2, s, s);
                g.DrawRectangle(Pens.Blue, p.X - s / 2, p.Y - s / 2, s, s);
            }
        }

        private PointF[] GetHandlePoints(RectangleF rect)
        {
            return new PointF[] {
                new PointF(rect.Left, rect.Top),
                new PointF(rect.Left + rect.Width/2, rect.Top),
                new PointF(rect.Right, rect.Top),
                new PointF(rect.Right, rect.Top + rect.Height/2),
                new PointF(rect.Right, rect.Bottom),
                new PointF(rect.Left + rect.Width/2, rect.Bottom),
                new PointF(rect.Left, rect.Bottom),
                new PointF(rect.Left, rect.Top + rect.Height/2)
            };
        }

        // 【修复】这里的 dpi 参数必须是实际 DPI，不能写死 96
        private RectangleF MMToPixelRect(RectangleF mmRect, float dpi)
        {
            float pixelPerMM = dpi / 25.4f;
            return new RectangleF(
                offsetX + mmRect.X * pixelPerMM * zoomScale,
                offsetY + mmRect.Y * pixelPerMM * zoomScale,
                mmRect.Width * pixelPerMM * zoomScale,
                mmRect.Height * pixelPerMM * zoomScale
            );
        }

        private RectangleF PixelToMMRect(Rectangle screenRect, float dpi)
        {
            float pixelPerMM = dpi / 25.4f;
            return new RectangleF(
                (screenRect.X - offsetX) / zoomScale / pixelPerMM,
                (screenRect.Y - offsetY) / zoomScale / pixelPerMM,
                screenRect.Width / zoomScale / pixelPerMM,
                screenRect.Height / zoomScale / pixelPerMM
            );
        }

        private Rectangle GetScreenRectFromPoints(Point p1, Point p2)
        {
            return new Rectangle(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
        }

        // 【修复】点击检测使用传入的 pixelPerMM，确保在任何屏幕上都准确
        private bool CheckHitElement(Point mouse, float pixelPerMM)
        {
            if (page == null) return false;
            // 倒序查找，优先选中最上层的
            for (int i = page.Elements.Count - 1; i >= 0; i--)
            {
                // 计算该元素当前的屏幕矩形
                float ex = page.Elements[i].Bounds.X * pixelPerMM * zoomScale + offsetX;
                float ey = page.Elements[i].Bounds.Y * pixelPerMM * zoomScale + offsetY;
                float ew = page.Elements[i].Bounds.Width * pixelPerMM * zoomScale;
                float eh = page.Elements[i].Bounds.Height * pixelPerMM * zoomScale;

                if (new RectangleF(ex, ey, ew, eh).Contains(mouse))
                {
                    selectedElement = page.Elements[i];
                    selectedElement.IsSelected = true;
                    // 取消其他选区
                    foreach (var o in page.Elements) if (o != selectedElement) o.IsSelected = false;
                    return true;
                }
            }
            return false;
        }

        private ResizeHandle CheckHitHandle(Point mouse, RectangleF rect)
        {
            int s = 10; // 感应区域大小
            if (new RectangleF(rect.Right - s, rect.Bottom - s, s * 2, s * 2).Contains(mouse)) return ResizeHandle.BottomRight;
            if (new RectangleF(rect.Right - s, rect.Top, s * 2, rect.Height).Contains(mouse)) return ResizeHandle.Right;
            if (new RectangleF(rect.Left, rect.Bottom - s, rect.Width, s * 2).Contains(mouse)) return ResizeHandle.Bottom;
            // ... 其它方向可按需添加
            return ResizeHandle.None;
        }

        private void UpdateCursor(Point mouse, float dpi)
        {
            if (activeMapFrame != null) { layoutBox.Cursor = Cursors.NoMove2D; return; }

            // 创建工具显示十字
            if (CurrentTool == LayoutTool.CreateMapFrame ||
                CurrentTool == LayoutTool.CreateNorthArrow ||
                CurrentTool == LayoutTool.CreateScaleBar)
            {
                layoutBox.Cursor = Cursors.Cross; return;
            }
            // 经纬网工具显示手型
            if (CurrentTool == LayoutTool.ToggleGrid) { layoutBox.Cursor = Cursors.Hand; return; }

            // 选中调整显示箭头
            if (selectedElement != null && selectedElement.IsSelected)
            {
                if (CheckHitHandle(mouse, MMToPixelRect(selectedElement.Bounds, dpi)) == ResizeHandle.BottomRight)
                {
                    layoutBox.Cursor = Cursors.SizeNWSE; return;
                }
            }
            layoutBox.Cursor = Cursors.Default;
        }
    }
}