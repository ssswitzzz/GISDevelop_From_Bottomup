using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using XGIS; // 引用你的核心命名空间

namespace GIS2025
{
    // ==========================================
    // 枚举定义
    // ==========================================
    public enum LayoutTool
    {
        None,           // 无
        PanPaper,       // 平移纸张 (中键)
        Select,         // 选择元素 (左键)
        CreateMapFrame, // 准备创建地图框
        ResizeElement,  // 正在缩放元素
        PanMapContent,  // 【激活状态】正在漫游地图框里面的内容
        CreateNorthArrow,
    }

    public enum ResizeHandle
    {
        None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left
    }

    public partial class LayoutControl : UserControl
    {
        // ==========================================
        // 核心数据变量
        // ==========================================
        private XLayoutPage page;

        // 视图变换变量
        private float zoomScale = 1.0f;
        private float offsetX = 40;
        private float offsetY = 40;

        // 交互状态变量
        public LayoutTool CurrentTool = LayoutTool.Select;
        private XLayoutElement selectedElement = null;
        private XMapFrame activeMapFrame = null; // 当前被激活的地图框

        // 拖拽/计算临时变量
        private Point mouseDownLoc;
        private RectangleF originalBounds;
        private ResizeHandle currentHandle = ResizeHandle.None;

        // 用于创建新 MapFrame 的缓存数据
        private List<XVectorLayer> _cacheLayers;
        private XTileLayer _cacheBaseLayer;
        private XExtent _cacheExtent;


        // UI 组件
        private ContextMenuStrip contextMenuLayout;

        public LayoutControl()
        {
            InitializeComponent();
            this.DoubleBuffered = true; // 防止闪烁

            // 初始化右键菜单
            InitContextMenu();

            // 绑定事件
            layoutBox.MouseWheel += LayoutBox_MouseWheel;
            layoutBox.MouseDown += LayoutBox_MouseDown;
            layoutBox.MouseMove += LayoutBox_MouseMove;
            layoutBox.MouseUp += LayoutBox_MouseUp;
            layoutBox.Paint += LayoutBox_Paint;
        }

        // ==========================================
        // 1. 初始化与数据加载
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

            // 动态显示逻辑
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
                    e.Cancel = true; // 不是地图框就不显示菜单
                }
            };
        }

        public void UpdateLayout(List<XVectorLayer> layers, XTileLayer baseLayer, XExtent currentExtent)
        {
            // 缓存数据，以便后续“拖拽创建”新地图框时使用
            _cacheLayers = layers;
            _cacheBaseLayer = baseLayer;
            _cacheExtent = currentExtent;

            // 如果是第一次加载，初始化纸张；否则保留当前纸张内容，只更新数据引用
            if (page == null)
            {
                page = new XLayoutPage(); // A4 纸
            }
            else
            {
                // 【核心修复】如果 Page 已经存在，遍历里面的 MapFrame，把数据更新一下
                foreach (var ele in page.Elements)
                {
                    if (ele is XMapFrame mapFrame)
                    {
                        // 更新图层列表 (这样新加的 Shapefile 就能看到了)
                        mapFrame.Layers = layers;
                        mapFrame.BaseLayer = baseLayer;

                        // 可选：是否要同步地图的视野范围？
                        // 通常专业软件不自动同步，除非用户要求。
                        // 如果你想每次切换都强制同步，取消下面注释：
                        // mapFrame.FrameView.Update(currentExtent, new Rectangle(0,0,1,1));
                    }
                }
            }

                // 强制重绘
                layoutBox.Invalidate();
        }

        // 供外部按钮调用：开始创建地图框
        public void StartCreateMapFrame()
        {
            CurrentTool = LayoutTool.CreateMapFrame;
            layoutBox.Cursor = Cursors.Cross; // 鼠标变十字
            // 取消当前选中
            selectedElement = null;
            activeMapFrame = null;
            layoutBox.Invalidate();
        }

        // 切换激活状态
        private void ToggleMapActivation(bool activate)
        {
            if (activate && selectedElement is XMapFrame mapFrame)
            {
                activeMapFrame = mapFrame;
                CurrentTool = LayoutTool.PanMapContent;
                layoutBox.Cursor = Cursors.NoMove2D; // 漫游图标
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
        // 2. 绘图逻辑 (Paint)
        // ==========================================
        private void LayoutBox_Paint(object sender, PaintEventArgs e)
        {
            if (page == null) return;

            // 获取当前 DPI
            float dpi = e.Graphics.DpiX;

            // A. 画纸张和所有元素
            page.Draw(e.Graphics, dpi, zoomScale, offsetX, offsetY);

            // B. 画选中框和控制点 (前提：没有激活任何地图框)
            if (activeMapFrame == null && selectedElement != null && selectedElement.IsSelected)
            {
                DrawSelectionHandles(e.Graphics, dpi);
            }

            // C. 画激活边框 (红色虚线框)
            if (activeMapFrame != null)
            {
                RectangleF screenRect = MMToPixelRect(activeMapFrame.Bounds, dpi);
                using (Pen p = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(p, screenRect.X - 2, screenRect.Y - 2, screenRect.Width + 4, screenRect.Height + 4);
                }
            }

            // D. 画创建过程中的橡皮筋框 (拖拽时的临时框)
            if ((CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow)
                && mouseDownLoc != Point.Empty && Control.MouseButtons == MouseButtons.Left)
            {
                Point currentMouse = layoutBox.PointToClient(Cursor.Position);
                Rectangle rect = GetScreenRectFromPoints(mouseDownLoc, currentMouse);
                using (Pen p = new Pen(Color.Blue, 1) { DashStyle = DashStyle.Dot })
                {
                    e.Graphics.DrawRectangle(p, rect);
                }
            }

            // E. 画标尺
            DrawRulers(e.Graphics, dpi, zoomScale, offsetX, offsetY);
        }

        // ==========================================
        // 3. 鼠标交互逻辑 (Mouse Events)
        // ==========================================
        private void LayoutBox_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDownLoc = e.Location;

            // 【修复 1】手动创建 Graphics 获取 DPI，因为 MouseEventArgs 没有 Graphics
            float dpi = 96;
            using (Graphics g = layoutBox.CreateGraphics())
            {
                dpi = g.DpiX;
            }
            float pixelPerMM = dpi / 25.4f;

            // --- 情况 1: 处于激活模式 ---
            if (activeMapFrame != null)
            {
                if (e.Button == MouseButtons.Right) contextMenuLayout.Show(layoutBox, e.Location);
                return;
            }

            // --- 情况 2: 创建模式 ---
            if (CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow)
            {
                // 如果是创建工具，不要执行下面的“选中”或“平移”逻辑，直接返回
                return;
            }


            // --- 情况 3: 右键菜单 ---
            if (e.Button == MouseButtons.Right)
            {
                if (CheckHitElement(e.Location, pixelPerMM))
                {
                    contextMenuLayout.Show(layoutBox, e.Location);
                }
                return;
            }

            // --- 情况 4: 左键操作 ---
            if (e.Button == MouseButtons.Left)
            {
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

                if (CheckHitElement(e.Location, pixelPerMM))
                {
                    CurrentTool = LayoutTool.Select;
                    originalBounds = selectedElement.Bounds;
                }
                else
                {
                    CurrentTool = LayoutTool.PanPaper;
                    selectedElement = null;
                    foreach (var ele in page.Elements) ele.IsSelected = false;
                    layoutBox.Invalidate();
                }
            }
            // --- 情况 5: 中键平移 ---
            else if (e.Button == MouseButtons.Middle)
            {
                CurrentTool = LayoutTool.PanPaper;
            }
        }

        private void LayoutBox_MouseMove(object sender, MouseEventArgs e)
        {
            // 【修复 1】手动获取 DPI
            float dpi = 96;
            using (Graphics g = layoutBox.CreateGraphics()) { dpi = g.DpiX; }
            float pixelPerMM = dpi / 25.4f;

            // 1. 【激活状态】漫游地图内容
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

            // 2. 【创建模式】
            if (CurrentTool == LayoutTool.CreateMapFrame && e.Button == MouseButtons.Left)
            {
                layoutBox.Invalidate();
                return;
            }

            // 3. 【调整大小】
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
                        // ... 可自行补充其他方向 ...
                }
                if (newBounds.Width < 5) newBounds.Width = 5;
                if (newBounds.Height < 5) newBounds.Height = 5;

                selectedElement.Bounds = newBounds;
                layoutBox.Invalidate();
            }

            // 4. 【移动元素】
            else if (CurrentTool == LayoutTool.Select && e.Button == MouseButtons.Left && selectedElement != null)
            {
                float dx = (e.X - mouseDownLoc.X) / zoomScale / pixelPerMM;
                float dy = (e.Y - mouseDownLoc.Y) / zoomScale / pixelPerMM;

                selectedElement.Bounds = new RectangleF(
                    originalBounds.X + dx, originalBounds.Y + dy,
                    originalBounds.Width, originalBounds.Height);
                layoutBox.Invalidate();
            }

            // 5. 【平移纸张】
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
            // 处理创建地图框的结束逻辑
            if (CurrentTool == LayoutTool.CreateMapFrame && e.Button == MouseButtons.Left)
            {
                float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) dpi = g.DpiX;
                float pixelPerMM = dpi / 25.4f;

                Rectangle screenRect = GetScreenRectFromPoints(mouseDownLoc, e.Location);

                if (screenRect.Width > 10 && screenRect.Height > 10)
                {
                    float x = (screenRect.X - offsetX) / zoomScale / pixelPerMM;
                    float y = (screenRect.Y - offsetY) / zoomScale / pixelPerMM;
                    float w = screenRect.Width / zoomScale / pixelPerMM;
                    float h = screenRect.Height / zoomScale / pixelPerMM;

                    if (_cacheLayers != null)
                    {
                        XExtent newExtent = _cacheExtent != null ? new XExtent(_cacheExtent) : new XExtent(0, 10, 0, 10);
                        XMapFrame newFrame = new XMapFrame(
                            new RectangleF(x, y, w, h),
                            _cacheLayers,
                            _cacheBaseLayer,
                            newExtent
                        );
                        page.Elements.Add(newFrame);

                        selectedElement = newFrame;
                        selectedElement.IsSelected = true;
                    }
                }

                CurrentTool = LayoutTool.Select;
                layoutBox.Cursor = Cursors.Default;
                layoutBox.Invalidate();
            }
            else if (activeMapFrame == null)
            {
                if (CurrentTool == LayoutTool.PanPaper) CurrentTool = LayoutTool.Select;
            }
            // === 新增：处理创建指北针 ===
            if (CurrentTool == LayoutTool.CreateNorthArrow && e.Button == MouseButtons.Left)
            {
                float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) dpi = g.DpiX;

                // 计算鼠标拖拽的屏幕矩形
                Rectangle screenRect = GetScreenRectFromPoints(mouseDownLoc, e.Location);
                RectangleF mmRect;

                // 判断是“点击”还是“框选”
                // 如果宽高小于 5 像素，视为点击，生成默认大小 (例如 20mm x 20mm)
                if (screenRect.Width < 5 || screenRect.Height < 5)
                {
                    // 将点击位置(screenRect.X, Y) 转换为毫米，作为左上角
                    RectangleF clickPt = PixelToMMRect(new Rectangle(e.X, e.Y, 0, 0), dpi);
                    mmRect = new RectangleF(clickPt.X, clickPt.Y, 20, 20); // 默认 20mm 大小
                }
                else
                {
                    // 框选大小
                    mmRect = PixelToMMRect(screenRect, dpi);
                }

                // 创建对象
                XNorthArrow arrow = new XNorthArrow(mmRect, _pendingNorthArrowStyle);
                page.Elements.Add(arrow);

                // 选中新对象
                selectedElement = arrow;
                selectedElement.IsSelected = true;

                // 恢复工具状态
                CurrentTool = LayoutTool.Select;
                layoutBox.Cursor = Cursors.Default;
                layoutBox.Invalidate();
                return; // 结束
            }
            // 【新增】在方法的最后，无条件重置交互状态
            currentHandle = ResizeHandle.None; // 停止缩放

            // 只有非激活状态下才重置工具，防止平移纸张后工具丢失
            if (activeMapFrame == null && CurrentTool != LayoutTool.Select)
            {
                if (CurrentTool == LayoutTool.PanPaper || CurrentTool == LayoutTool.ResizeElement)
                    CurrentTool = LayoutTool.Select;
            }

            layoutBox.Invalidate();
        }

        private void LayoutBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (activeMapFrame != null)
            {
                // 【修复 2】使用 CurrentMapExtent 来调用 ZoomToCenter
                bool isZoomIn = e.Delta > 0;
                XVertex center = activeMapFrame.FrameView.ToMapVertex(e.Location);

                // 注意：这里我们直接操作 Extent
                activeMapFrame.FrameView.CurrentMapExtent.ZoomToCenter(center, isZoomIn ? 1.1 : 0.9);

                layoutBox.Invalidate(); // 下一次 Paint 时会通过 Draw 自动更新 View 的窗口参数
            }
            else
            {
                // 缩放纸张
                if (e.Delta > 0) zoomScale *= 1.1f;
                else zoomScale /= 1.1f;
                layoutBox.Invalidate();
            }
        }

        // ==========================================
        // 4. 辅助与绘图工具方法
        // ==========================================

        private const int RulerThickness = 25;
        private void DrawRulers(Graphics g, float dpi, float zoom, float offX, float offY)
        {
            float pixelPerMM = dpi / 25.4f;
            Pen linePen = Pens.Black;
            Font rulerFont = new Font("Arial", 7);
            Brush textBrush = Brushes.Black;

            g.FillRectangle(Brushes.WhiteSmoke, RulerThickness, 0, layoutBox.Width, RulerThickness);
            g.FillRectangle(Brushes.WhiteSmoke, 0, RulerThickness, RulerThickness, layoutBox.Height);
            g.FillRectangle(Brushes.LightGray, 0, 0, RulerThickness, RulerThickness);
            g.DrawLine(Pens.Gray, 0, RulerThickness, layoutBox.Width, RulerThickness);
            g.DrawLine(Pens.Gray, RulerThickness, 0, RulerThickness, layoutBox.Height);

            for (int i = 0; i <= 500; i += 1)
            {
                float sx = offX + (i * pixelPerMM * zoom);
                if (sx > RulerThickness && sx < layoutBox.Width)
                {
                    if (i % 10 == 0)
                    {
                        g.DrawLine(linePen, sx, 0, sx, 15);
                        string t = i.ToString();
                        g.DrawString(t, rulerFont, textBrush, sx - g.MeasureString(t, rulerFont).Width / 2, 12);
                    }
                    else if (i % 5 == 0 && zoom > 0.5) g.DrawLine(linePen, sx, 10, sx, 15);
                }
            }

            for (int i = 0; i <= 500; i += 1)
            {
                float sy = offY + (i * pixelPerMM * zoom);
                if (sy > RulerThickness && sy < layoutBox.Height)
                {
                    if (i % 10 == 0)
                    {
                        g.DrawLine(linePen, 0, sy, 15, sy);
                        g.DrawString(i.ToString(), rulerFont, textBrush, 2, sy - 6);
                    }
                    else if (i % 5 == 0 && zoom > 0.5) g.DrawLine(linePen, 10, sy, 15, sy);
                }
            }
        }

        private void DrawSelectionHandles(Graphics g, float dpi)
        {
            RectangleF rect = MMToPixelRect(selectedElement.Bounds, dpi);
            int size = 6;
            PointF[] pts = GetHandlePoints(rect);
            foreach (PointF p in pts)
            {
                g.FillRectangle(Brushes.White, p.X - size / 2, p.Y - size / 2, size, size);
                g.DrawRectangle(Pens.Blue, p.X - size / 2, p.Y - size / 2, size, size);
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

        private bool CheckHitElement(Point mouseLoc, float pixelPerMM)
        {
            for (int i = page.Elements.Count - 1; i >= 0; i--)
            {
                var ele = page.Elements[i];
                // 计算该元素当前的屏幕矩形 (这里重复计算是为了获取准确的判定区域)
                float ex = ele.Bounds.X * pixelPerMM * zoomScale + offsetX;
                float ey = ele.Bounds.Y * pixelPerMM * zoomScale + offsetY;
                float ew = ele.Bounds.Width * pixelPerMM * zoomScale;
                float eh = ele.Bounds.Height * pixelPerMM * zoomScale;

                if (new RectangleF(ex, ey, ew, eh).Contains(mouseLoc))
                {
                    selectedElement = ele;
                    selectedElement.IsSelected = true;
                    foreach (var o in page.Elements) if (o != ele) o.IsSelected = false;
                    return true;
                }
            }
            return false;
        }

        private ResizeHandle CheckHitHandle(Point mouse, RectangleF rect)
        {
            int size = 10;
            if (new RectangleF(rect.Right - size, rect.Bottom - size, size * 2, size * 2).Contains(mouse))
                return ResizeHandle.BottomRight;
            if (new RectangleF(rect.Right - size, rect.Top, size * 2, rect.Height).Contains(mouse))
                return ResizeHandle.Right;
            if (new RectangleF(rect.Left, rect.Bottom - size, rect.Width, size * 2).Contains(mouse))
                return ResizeHandle.Bottom;
            if (new RectangleF(rect.Left - size, rect.Top - size, size * 2, size * 2).Contains(mouse))
                return ResizeHandle.TopLeft;

            return ResizeHandle.None;
        }

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

        private void UpdateCursor(Point mouse, float dpi)
        {
            if (activeMapFrame != null)
            {
                layoutBox.Cursor = Cursors.NoMove2D;
                return;
            }
            if (CurrentTool == LayoutTool.CreateMapFrame)
            {
                layoutBox.Cursor = Cursors.Cross;
                return;
            }
            if (selectedElement != null && selectedElement.IsSelected)
            {
                var handle = CheckHitHandle(mouse, MMToPixelRect(selectedElement.Bounds, dpi));
                if (handle == ResizeHandle.BottomRight) { layoutBox.Cursor = Cursors.SizeNWSE; return; }
                if (handle == ResizeHandle.Right) { layoutBox.Cursor = Cursors.SizeWE; return; }
                if (handle == ResizeHandle.Bottom) { layoutBox.Cursor = Cursors.SizeNS; return; }
            }
            if (CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow)
            {
                layoutBox.Cursor = Cursors.Cross;
                return;
            }
            layoutBox.Cursor = Cursors.Default;
        }

        private Rectangle GetScreenRectFromPoints(Point p1, Point p2)
        {
            return new Rectangle(
                Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
        }
        // 临时存储要创建的样式
        private NorthArrowStyle _pendingNorthArrowStyle;

        // 【外部调用】开始创建指北针
        public void StartCreateNorthArrow(NorthArrowStyle style)
        {
            CurrentTool = LayoutTool.CreateNorthArrow;
            _pendingNorthArrowStyle = style;
            layoutBox.Cursor = Cursors.Cross;
            // 取消当前选中
            selectedElement = null;
            activeMapFrame = null;
            layoutBox.Invalidate();
        }

        // 【辅助函数】将屏幕像素反算回纸张毫米坐标
        private RectangleF PixelToMMRect(Rectangle screenRect, float dpi)
        {
            float pixelPerMM = dpi / 25.4f;
            float x = (screenRect.X - offsetX) / zoomScale / pixelPerMM;
            float y = (screenRect.Y - offsetY) / zoomScale / pixelPerMM;
            float w = screenRect.Width / zoomScale / pixelPerMM;
            float h = screenRect.Height / zoomScale / pixelPerMM;
            return new RectangleF(x, y, w, h);
        }
    }
}