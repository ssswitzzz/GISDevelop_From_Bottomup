using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging; // 导出功能需要这个
using System.Windows.Forms;
using System.Linq;
using XGIS;

namespace GIS2025
{
    public enum LayoutTool
    {
        None, PanPaper, Select, CreateMapFrame, ResizeElement, PanMapContent,
        CreateNorthArrow, CreateScaleBar, ToggleGrid, CreateText,
        CreateLegend // 【新增】图例工具
    }

    public enum ResizeHandle { None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }

    public struct SmartGuide
    {
        public bool IsVertical;
        public float Position;
        public float Start;
        public float End;
    }

    public partial class LayoutControl : UserControl
    {
        public XLayoutPage Page;
        public event EventHandler ElementChanged;
        public event EventHandler SelectionChanged;

        private float zoomScale = 1.0f;
        private float offsetX = 40;
        private float offsetY = 40;

        public LayoutTool CurrentTool = LayoutTool.Select;
        private XLayoutElement selectedElement = null;
        public XLayoutElement SelectedElement => selectedElement;

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

        // 吸附相关
        private List<SmartGuide> activeGuides = new List<SmartGuide>();
        private const int SNAP_THRESHOLD_PIXEL = 10;
        private const float MARGIN_MM = 10.0f;

        public LayoutControl()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.Page = new XLayoutPage();

            InitContextMenu();

            // 绑定事件
            layoutBox.MouseWheel += LayoutBox_MouseWheel;
            layoutBox.MouseDown += LayoutBox_MouseDown;
            layoutBox.MouseMove += LayoutBox_MouseMove;
            layoutBox.MouseUp += LayoutBox_MouseUp;
            layoutBox.Paint += LayoutBox_Paint;
            layoutBox.MouseDoubleClick += LayoutBox_MouseDoubleClick;
            this.KeyDown += LayoutControl_KeyDown;
        }

        private void NotifyElementChanged() { ElementChanged?.Invoke(this, EventArgs.Empty); }
        private void NotifySelectionChanged() { SelectionChanged?.Invoke(this, EventArgs.Empty); }

        public void SelectElement(XLayoutElement ele)
        {
            selectedElement = ele;
            foreach (var e in Page.Elements) e.IsSelected = (e == selectedElement);
            if (activeMapFrame != null && activeMapFrame != selectedElement) activeMapFrame = null;
            layoutBox.Invalidate();
        }

        private void InitContextMenu()
        {
            contextMenuLayout = new ContextMenuStrip();
            var btnActivate = new ToolStripMenuItem("激活地图框 (进入漫游模式)");
            btnActivate.Click += (s, e) => { ToggleMapActivation(true); };
            var btnCloseActivate = new ToolStripMenuItem("退出激活 (返回布局模式)");
            btnCloseActivate.Click += (s, e) => { ToggleMapActivation(false); };
            contextMenuLayout.Items.Add(btnActivate);
            contextMenuLayout.Items.Add(btnCloseActivate);
            contextMenuLayout.Opening += (s, e) => {
                if (selectedElement is XMapFrame)
                {
                    bool isActive = (activeMapFrame == selectedElement);
                    btnActivate.Visible = !isActive;
                    btnCloseActivate.Visible = isActive;
                }
                else e.Cancel = true;
            };
        }

        public void UpdateLayout(List<XVectorLayer> layers, XTileLayer baseLayer, XExtent currentExtent)
        {
            _cacheLayers = layers;
            _cacheBaseLayer = baseLayer;
            _cacheExtent = currentExtent;
            foreach (var ele in Page.Elements)
            {
                if (ele is XMapFrame mapFrame)
                {
                    mapFrame.Layers = layers;
                    mapFrame.BaseLayer = baseLayer;
                }
            }
            layoutBox.Invalidate();
        }

        // 工具方法
        public void StartCreateMapFrame() { CurrentTool = LayoutTool.CreateMapFrame; layoutBox.Cursor = Cursors.Cross; DeselectAll(); }
        public void StartCreateNorthArrow(NorthArrowStyle style) { CurrentTool = LayoutTool.CreateNorthArrow; _pendingNorthArrowStyle = style; layoutBox.Cursor = Cursors.Cross; DeselectAll(); }
        public void StartCreateScaleBar(ScaleBarStyle style) { CurrentTool = LayoutTool.CreateScaleBar; _pendingScaleBarStyle = style; layoutBox.Cursor = Cursors.Cross; DeselectAll(); }
        public void StartToggleGrid() { CurrentTool = LayoutTool.ToggleGrid; layoutBox.Cursor = Cursors.Hand; DeselectAll(); }
        public void StartCreateText() { CurrentTool = LayoutTool.CreateText; layoutBox.Cursor = Cursors.IBeam; DeselectAll(); }

        // 【新增】开始创建图例
        public void StartCreateLegend() { CurrentTool = LayoutTool.CreateLegend; layoutBox.Cursor = Cursors.Cross; DeselectAll(); }

        private void DeselectAll()
        {
            selectedElement = null;
            activeMapFrame = null;
            foreach (var ele in Page.Elements) ele.IsSelected = false;
            layoutBox.Invalidate();
            NotifySelectionChanged();
        }

        private void LayoutControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && selectedElement != null && activeMapFrame == null)
            {
                Page.Elements.Remove(selectedElement);
                selectedElement = null;
                layoutBox.Invalidate();
                NotifyElementChanged();
            }
        }

        // ================= 鼠标事件 =================
        private void LayoutBox_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();
            mouseDownLoc = e.Location;
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) { dpi = g.DpiX; }
            float pixelPerMM = dpi / 25.4f;

            if (activeMapFrame != null)
            {
                if (e.Button == MouseButtons.Right) contextMenuLayout.Show(layoutBox, e.Location);
                return;
            }

            // 【修改】加入 CreateLegend 到屏蔽列表
            if (CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow ||
                CurrentTool == LayoutTool.CreateScaleBar || CurrentTool == LayoutTool.ToggleGrid ||
                CurrentTool == LayoutTool.CreateText || CurrentTool == LayoutTool.CreateLegend) return;

            if (e.Button == MouseButtons.Right)
            {
                if (CheckHitElement(e.Location, pixelPerMM))
                {
                    NotifySelectionChanged();
                    contextMenuLayout.Show(layoutBox, e.Location);
                }
                return;
            }

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
                    NotifySelectionChanged();
                }
                else
                {
                    CurrentTool = LayoutTool.PanPaper;
                    selectedElement = null;
                    foreach (var ele in Page.Elements) ele.IsSelected = false;
                    layoutBox.Invalidate();
                    NotifySelectionChanged();
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                CurrentTool = LayoutTool.PanPaper;
            }
        }

        private void LayoutBox_MouseMove(object sender, MouseEventArgs e)
        {
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) { dpi = g.DpiX; }
            float pixelPerMM = dpi / 25.4f;

            // 1. 地图漫游模式
            if (activeMapFrame != null)
            {
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
                {
                    RectangleF screenRectF = MMToPixelRect(activeMapFrame.Bounds, dpi);
                    activeMapFrame.FrameView.UpdateMapWindow(new Rectangle(0, 0, (int)screenRectF.Width, (int)screenRectF.Height));

                    XVertex v1 = activeMapFrame.FrameView.ToMapVertex(mouseDownLoc);
                    XVertex v2 = activeMapFrame.FrameView.ToMapVertex(e.Location);

                    activeMapFrame.FrameView.OffsetCenter(v1, v2);
                    activeMapFrame.StableExtent = new XExtent(activeMapFrame.FrameView.CurrentMapExtent);

                    mouseDownLoc = e.Location;
                    layoutBox.Invalidate();
                }
                return;
            }

            // 2. 创建元素模式
            // 【修改】加入 CreateLegend
            bool isCreating = CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow ||
                              CurrentTool == LayoutTool.CreateScaleBar || CurrentTool == LayoutTool.CreateText ||
                              CurrentTool == LayoutTool.CreateLegend;

            if (isCreating && e.Button == MouseButtons.Left)
            {
                layoutBox.Invalidate();
                return;
            }

            // 3. Resize 模式
            if (CurrentTool == LayoutTool.ResizeElement && selectedElement != null)
            {
                float dx = (e.X - mouseDownLoc.X) / zoomScale / pixelPerMM;
                float dy = (e.Y - mouseDownLoc.Y) / zoomScale / pixelPerMM;

                float newX = originalBounds.X;
                float newY = originalBounds.Y;
                float newW = originalBounds.Width;
                float newH = originalBounds.Height;

                if (currentHandle == ResizeHandle.Left || currentHandle == ResizeHandle.TopLeft || currentHandle == ResizeHandle.BottomLeft) { newX += dx; newW -= dx; }
                if (currentHandle == ResizeHandle.Right || currentHandle == ResizeHandle.TopRight || currentHandle == ResizeHandle.BottomRight) { newW += dx; }
                if (currentHandle == ResizeHandle.Top || currentHandle == ResizeHandle.TopLeft || currentHandle == ResizeHandle.TopRight) { newY += dy; newH -= dy; }
                if (currentHandle == ResizeHandle.Bottom || currentHandle == ResizeHandle.BottomLeft || currentHandle == ResizeHandle.BottomRight) { newH += dy; }

                if (newW < 5)
                {
                    if (currentHandle == ResizeHandle.Left || currentHandle == ResizeHandle.TopLeft || currentHandle == ResizeHandle.BottomLeft)
                        newX = originalBounds.X + originalBounds.Width - 5;
                    newW = 5;
                }
                if (newH < 5)
                {
                    if (currentHandle == ResizeHandle.Top || currentHandle == ResizeHandle.TopLeft || currentHandle == ResizeHandle.TopRight)
                        newY = originalBounds.Y + originalBounds.Height - 5;
                    newH = 5;
                }

                RectangleF newBounds = new RectangleF(newX, newY, newW, newH);
                newBounds = CheckAndApplySnap(newBounds, pixelPerMM);
                selectedElement.Bounds = newBounds;
                layoutBox.Invalidate();
            }
            // 4. Move 模式
            else if (CurrentTool == LayoutTool.Select && e.Button == MouseButtons.Left && selectedElement != null)
            {
                float dx = (e.X - mouseDownLoc.X) / zoomScale / pixelPerMM;
                float dy = (e.Y - mouseDownLoc.Y) / zoomScale / pixelPerMM;
                RectangleF proposedBounds = new RectangleF(originalBounds.X + dx, originalBounds.Y + dy, originalBounds.Width, originalBounds.Height);
                selectedElement.Bounds = CheckAndApplySnap(proposedBounds, pixelPerMM);
                layoutBox.Invalidate();
            }
            // 5. Pan Paper 模式
            else if (CurrentTool == LayoutTool.PanPaper && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle))
            {
                offsetX += e.X - mouseDownLoc.X;
                offsetY += e.Y - mouseDownLoc.Y;
                mouseDownLoc = e.Location;
                layoutBox.Invalidate();
            }
            UpdateCursor(e.Location, dpi);
        }

        private RectangleF CheckAndApplySnap(RectangleF proposedRect, float pixelPerMM)
        {
            activeGuides.Clear();
            float snapDistMM = SNAP_THRESHOLD_PIXEL / (zoomScale * pixelPerMM);
            float resultX = proposedRect.X;
            float resultY = proposedRect.Y;

            List<float> targetX = new List<float> { 0, Page.WidthMM / 2, Page.WidthMM, MARGIN_MM, Page.WidthMM - MARGIN_MM };
            List<float> targetY = new List<float> { 0, Page.HeightMM / 2, Page.HeightMM, MARGIN_MM, Page.HeightMM - MARGIN_MM };

            float[] elementXOffsets = { 0, proposedRect.Width / 2, proposedRect.Width };
            float[] elementYOffsets = { 0, proposedRect.Height / 2, proposedRect.Height };

            bool snappedX = false, snappedY = false;

            for (int i = 0; i < elementXOffsets.Length; i++)
            {
                if (snappedX) break;
                float currentEleX = proposedRect.X + elementXOffsets[i];
                foreach (float tx in targetX)
                {
                    if (Math.Abs(currentEleX - tx) < snapDistMM)
                    {
                        resultX = tx - elementXOffsets[i];
                        activeGuides.Add(new SmartGuide { IsVertical = true, Position = tx, Start = 0, End = Page.HeightMM });
                        snappedX = true;
                        break;
                    }
                }
            }

            for (int i = 0; i < elementYOffsets.Length; i++)
            {
                if (snappedY) break;
                float currentEleY = proposedRect.Y + elementYOffsets[i];
                foreach (float ty in targetY)
                {
                    if (Math.Abs(currentEleY - ty) < snapDistMM)
                    {
                        resultY = ty - elementYOffsets[i];
                        activeGuides.Add(new SmartGuide { IsVertical = false, Position = ty, Start = 0, End = Page.WidthMM });
                        snappedY = true;
                        break;
                    }
                }
            }
            return new RectangleF(resultX, resultY, proposedRect.Width, proposedRect.Height);
        }

        private void LayoutBox_MouseUp(object sender, MouseEventArgs e)
        {
            activeGuides.Clear();
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) dpi = g.DpiX;
            Rectangle screenRect = GetScreenRectFromPoints(mouseDownLoc, e.Location);

            Action<XLayoutElement> FinishCreation = (newEle) => {
                Page.Elements.Add(newEle);
                selectedElement = newEle;
                selectedElement.IsSelected = true;
                CurrentTool = LayoutTool.Select;
                layoutBox.Cursor = Cursors.Default;
                layoutBox.Invalidate();
                NotifyElementChanged();
                NotifySelectionChanged();
            };

            // 创建文字逻辑
            if (CurrentTool == LayoutTool.CreateText && e.Button == MouseButtons.Left)
            {
                string txt = ShowInputBox("请输入文本内容:", "添加文字", "Text");
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    RectangleF mmRect;
                    if (screenRect.Width < 5)
                    {
                        RectangleF p = PixelToMMRect(new Rectangle(e.X, e.Y, 0, 0), dpi);
                        mmRect = new RectangleF(p.X, p.Y, 50, 10);
                    }
                    else { mmRect = PixelToMMRect(screenRect, dpi); }
                    FinishCreation(new XTextElement(mmRect, txt));
                }
                else { CurrentTool = LayoutTool.Select; }
                layoutBox.Invalidate();
                return;
            }

            // 创建地图框
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
                CurrentTool = LayoutTool.Select; layoutBox.Invalidate();
            }
            // 创建指北针
            else if (CurrentTool == LayoutTool.CreateNorthArrow && e.Button == MouseButtons.Left)
            {
                RectangleF mmRect;
                if (screenRect.Width < 5) { RectangleF p = PixelToMMRect(new Rectangle(e.X, e.Y, 0, 0), dpi); mmRect = new RectangleF(p.X, p.Y, 20, 20); }
                else mmRect = PixelToMMRect(screenRect, dpi);
                FinishCreation(new XNorthArrow(mmRect, _pendingNorthArrowStyle));
                return;
            }
            // 创建比例尺
            else if (CurrentTool == LayoutTool.CreateScaleBar && e.Button == MouseButtons.Left)
            {
                XMapFrame target = null;
                if (activeMapFrame != null) target = activeMapFrame;
                else { foreach (var ele in Page.Elements) if (ele is XMapFrame mf) { target = mf; break; } }
                if (target == null) { MessageBox.Show("请先创建地图框！"); CurrentTool = LayoutTool.Select; layoutBox.Cursor = Cursors.Default; layoutBox.Invalidate(); return; }
                RectangleF mmRect;
                if (screenRect.Width < 5) { RectangleF p = PixelToMMRect(new Rectangle(e.X, e.Y, 0, 0), dpi); mmRect = new RectangleF(p.X, p.Y, 40, 10); }
                else mmRect = PixelToMMRect(screenRect, dpi);
                FinishCreation(new XScaleBar(mmRect, _pendingScaleBarStyle, target));
                return;
            }
            // 创建图例 【新增】
            else if (CurrentTool == LayoutTool.CreateLegend && e.Button == MouseButtons.Left)
            {
                // 1. 寻找关联的地图框
                XMapFrame target = activeMapFrame;
                if (target == null) target = Page.Elements.FirstOrDefault(x => x is XMapFrame) as XMapFrame;

                if (target == null)
                {
                    MessageBox.Show("请先添加一个地图框！");
                    CurrentTool = LayoutTool.Select;
                    layoutBox.Cursor = Cursors.Default;
                    layoutBox.Invalidate();
                    return;
                }

                // 2. 计算大小
                RectangleF mmRect;
                if (screenRect.Width < 5)
                {
                    RectangleF p = PixelToMMRect(new Rectangle(e.X, e.Y, 0, 0), dpi);
                    mmRect = new RectangleF(p.X, p.Y, 50, 80); // 默认尺寸
                }
                else { mmRect = PixelToMMRect(screenRect, dpi); }

                // 3. 创建图例
                FinishCreation(new LayoutLegend(target) { Bounds = mmRect });
                return;
            }
            // 经纬网开关
            else if (CurrentTool == LayoutTool.ToggleGrid && e.Button == MouseButtons.Left)
            {
                if (CheckHitElement(e.Location, dpi / 25.4f) && selectedElement is XMapFrame mapFrame)
                {
                    mapFrame.ShowGrid = !mapFrame.ShowGrid;
                    CurrentTool = LayoutTool.Select; layoutBox.Cursor = Cursors.Default; layoutBox.Invalidate();
                    return;
                }
            }

            currentHandle = ResizeHandle.None;
            if (activeMapFrame == null && CurrentTool != LayoutTool.Select)
            {
                if (CurrentTool == LayoutTool.PanPaper || CurrentTool == LayoutTool.ResizeElement) CurrentTool = LayoutTool.Select;
            }
            layoutBox.Invalidate();
        }

        private void LayoutBox_Paint(object sender, PaintEventArgs e)
        {
            if (Page == null) return;
            float dpi = e.Graphics.DpiX;
            float pixelPerMM = dpi / 25.4f;

            // 【关键】Page.Draw 需要传入当前屏幕 DPI，以便在屏幕上正确显示
            Page.Draw(e.Graphics, dpi, zoomScale, offsetX, offsetY);

            if (activeMapFrame == null && selectedElement != null && selectedElement.IsSelected) DrawSelectionHandles(e.Graphics, dpi);
            if (activeMapFrame != null)
            {
                RectangleF rect = MMToPixelRect(activeMapFrame.Bounds, dpi);
                using (Pen p = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash }) e.Graphics.DrawRectangle(p, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
            }

            // 【修改】加入 CreateLegend 绘制拖拽框
            bool isCreating = CurrentTool == LayoutTool.CreateMapFrame ||
                              CurrentTool == LayoutTool.CreateNorthArrow ||
                              CurrentTool == LayoutTool.CreateScaleBar ||
                              CurrentTool == LayoutTool.CreateText ||
                              CurrentTool == LayoutTool.CreateLegend;

            if (isCreating && mouseDownLoc != Point.Empty && Control.MouseButtons == MouseButtons.Left)
            {
                Point cur = layoutBox.PointToClient(Cursor.Position);
                Rectangle rect = GetScreenRectFromPoints(mouseDownLoc, cur);
                using (Pen p = new Pen(Color.Blue, 1) { DashStyle = DashStyle.Dot }) e.Graphics.DrawRectangle(p, rect);
            }
            DrawRulers(e.Graphics, dpi, zoomScale, offsetX, offsetY);

            // 绘制智能吸附线
            if (activeGuides.Count > 0 && (CurrentTool == LayoutTool.Select || CurrentTool == LayoutTool.ResizeElement))
            {
                using (Pen guidePen = new Pen(Color.DeepSkyBlue, 1) { DashStyle = DashStyle.DashDot })
                {
                    foreach (var guide in activeGuides)
                    {
                        if (guide.IsVertical)
                        {
                            float x = offsetX + guide.Position * pixelPerMM * zoomScale;
                            float y1 = offsetY + guide.Start * pixelPerMM * zoomScale;
                            float y2 = offsetY + guide.End * pixelPerMM * zoomScale;
                            e.Graphics.DrawLine(guidePen, x, y1, x, y2);
                        }
                        else
                        {
                            float y = offsetY + guide.Position * pixelPerMM * zoomScale;
                            float x1 = offsetX + guide.Start * pixelPerMM * zoomScale;
                            float x2 = offsetX + guide.End * pixelPerMM * zoomScale;
                            e.Graphics.DrawLine(guidePen, x1, y, x2, y);
                        }
                    }
                }
            }
        }

        private void LayoutBox_MouseWheel(object sender, MouseEventArgs e)
        {
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) { dpi = g.DpiX; }

            if (activeMapFrame != null)
            {
                RectangleF screenRectF = MMToPixelRect(activeMapFrame.Bounds, dpi);
                int localX = (int)(e.X - screenRectF.X);
                int localY = (int)(e.Y - screenRectF.Y);

                activeMapFrame.FrameView.UpdateMapWindow(new Rectangle(0, 0, (int)screenRectF.Width, (int)screenRectF.Height));

                XVertex anchor = activeMapFrame.FrameView.ToMapVertex(new Point(localX, localY));
                bool isZoomIn = e.Delta > 0;
                double ratio = isZoomIn ? 1.1 : (1.0 / 1.1);

                activeMapFrame.FrameView.CurrentMapExtent.ZoomToCenter(anchor, ratio);
                activeMapFrame.StableExtent = new XExtent(activeMapFrame.FrameView.CurrentMapExtent);

                layoutBox.Invalidate();
            }
            else
            {
                float mouseX_World = (e.X - offsetX) / zoomScale;
                float mouseY_World = (e.Y - offsetY) / zoomScale;
                if (e.Delta > 0) zoomScale *= 1.1f; else zoomScale /= 1.1f;
                if (zoomScale < 0.1f) zoomScale = 0.1f; if (zoomScale > 10.0f) zoomScale = 10.0f;
                offsetX = e.X - mouseX_World * zoomScale;
                offsetY = e.Y - mouseY_World * zoomScale;
                layoutBox.Invalidate();
            }
        }
        private void LayoutBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (CurrentTool == LayoutTool.Select && selectedElement != null && e.Button == MouseButtons.Left)
            {
                if (selectedElement is XTextElement textEle)
                {
                    FormTextProperty fm = new FormTextProperty(textEle);
                    if (fm.ShowDialog() == DialogResult.OK)
                    {
                        layoutBox.Invalidate();
                        NotifyElementChanged();
                    }
                }
            }
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

        private void DrawRulers(Graphics g, float dpi, float zoom, float offX, float offY)
        {
            float pixelPerMM = dpi / 25.4f;
            int thick = 25;
            g.FillRectangle(Brushes.WhiteSmoke, thick, 0, layoutBox.Width, thick);
            g.FillRectangle(Brushes.WhiteSmoke, 0, thick, thick, layoutBox.Height);
            g.DrawLine(Pens.Gray, 0, thick, layoutBox.Width, thick);
            g.DrawLine(Pens.Gray, thick, 0, thick, layoutBox.Height);
            Font f = new Font("Arial", 7); Brush b = Brushes.Black;
            for (int i = 0; i <= 500; i++)
            {
                float sx = offX + (i * pixelPerMM * zoom);
                if (sx > thick && sx < layoutBox.Width)
                {
                    if (i % 10 == 0) { g.DrawLine(Pens.Black, sx, 0, sx, 15); g.DrawString(i.ToString(), f, b, sx - 5, 12); }
                    else if (i % 5 == 0 && zoom > 0.5) g.DrawLine(Pens.Black, sx, 10, sx, 15);
                }
                float sy = offY + (i * pixelPerMM * zoom);
                if (sy > thick && sy < layoutBox.Height)
                {
                    if (i % 10 == 0) { g.DrawLine(Pens.Black, 0, sy, 15, sy); g.DrawString(i.ToString(), f, b, 2, sy - 6); }
                    else if (i % 5 == 0 && zoom > 0.5) g.DrawLine(Pens.Black, 10, sy, 15, sy);
                }
            }
        }

        private void DrawSelectionHandles(Graphics g, float dpi)
        {
            RectangleF rect = MMToPixelRect(selectedElement.Bounds, dpi);
            int s = 6;
            PointF[] handles = GetHandlePoints(rect);
            foreach (PointF p in handles)
            {
                g.FillRectangle(Brushes.White, p.X - s / 2, p.Y - s / 2, s, s);
                g.DrawRectangle(Pens.Blue, p.X - s / 2, p.Y - s / 2, s, s);
            }
        }

        private PointF[] GetHandlePoints(RectangleF rect)
        {
            return new PointF[] {
                new PointF(rect.Left, rect.Top),
                new PointF(rect.Left + rect.Width / 2, rect.Top),
                new PointF(rect.Right, rect.Top),
                new PointF(rect.Right, rect.Top + rect.Height / 2),
                new PointF(rect.Right, rect.Bottom),
                new PointF(rect.Left + rect.Width / 2, rect.Bottom),
                new PointF(rect.Left, rect.Bottom),
                new PointF(rect.Left, rect.Top + rect.Height / 2)
            };
        }

        private RectangleF MMToPixelRect(RectangleF mmRect, float dpi)
        {
            float pixelPerMM = dpi / 25.4f;
            return new RectangleF(offsetX + mmRect.X * pixelPerMM * zoomScale, offsetY + mmRect.Y * pixelPerMM * zoomScale, mmRect.Width * pixelPerMM * zoomScale, mmRect.Height * pixelPerMM * zoomScale);
        }

        private RectangleF PixelToMMRect(Rectangle screenRect, float dpi)
        {
            float pixelPerMM = dpi / 25.4f;
            return new RectangleF((screenRect.X - offsetX) / zoomScale / pixelPerMM, (screenRect.Y - offsetY) / zoomScale / pixelPerMM, screenRect.Width / zoomScale / pixelPerMM, screenRect.Height / zoomScale / pixelPerMM);
        }

        private Rectangle GetScreenRectFromPoints(Point p1, Point p2) { return new Rectangle(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y)); }

        private bool CheckHitElement(Point mouse, float pixelPerMM)
        {
            for (int i = Page.Elements.Count - 1; i >= 0; i--)
            {
                float x = Page.Elements[i].Bounds.X * pixelPerMM * zoomScale + offsetX;
                float y = Page.Elements[i].Bounds.Y * pixelPerMM * zoomScale + offsetY;
                float w = Page.Elements[i].Bounds.Width * pixelPerMM * zoomScale;
                float h = Page.Elements[i].Bounds.Height * pixelPerMM * zoomScale;
                if (new RectangleF(x, y, w, h).Contains(mouse))
                {
                    selectedElement = Page.Elements[i];
                    selectedElement.IsSelected = true;
                    foreach (var o in Page.Elements) if (o != selectedElement) o.IsSelected = false;
                    return true;
                }
            }
            return false;
        }

        private ResizeHandle CheckHitHandle(Point mouse, RectangleF rect)
        {
            int s = 10;
            if (new RectangleF(rect.Left - s, rect.Top - s, s * 2, s * 2).Contains(mouse)) return ResizeHandle.TopLeft;
            if (new RectangleF(rect.Right - s, rect.Top - s, s * 2, s * 2).Contains(mouse)) return ResizeHandle.TopRight;
            if (new RectangleF(rect.Left - s, rect.Bottom - s, s * 2, s * 2).Contains(mouse)) return ResizeHandle.BottomLeft;
            if (new RectangleF(rect.Right - s, rect.Bottom - s, s * 2, s * 2).Contains(mouse)) return ResizeHandle.BottomRight;
            if (new RectangleF(rect.Left, rect.Top - s, rect.Width, s * 2).Contains(mouse)) return ResizeHandle.Top;
            if (new RectangleF(rect.Left, rect.Bottom - s, rect.Width, s * 2).Contains(mouse)) return ResizeHandle.Bottom;
            if (new RectangleF(rect.Left - s, rect.Top, s * 2, rect.Height).Contains(mouse)) return ResizeHandle.Left;
            if (new RectangleF(rect.Right - s, rect.Top, s * 2, rect.Height).Contains(mouse)) return ResizeHandle.Right;
            return ResizeHandle.None;
        }

        private void UpdateCursor(Point mouse, float dpi)
        {
            if (activeMapFrame != null) { layoutBox.Cursor = Cursors.NoMove2D; return; }
            if (CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow || CurrentTool == LayoutTool.CreateScaleBar || CurrentTool == LayoutTool.CreateText || CurrentTool == LayoutTool.CreateLegend) { layoutBox.Cursor = Cursors.Cross; return; }
            if (CurrentTool == LayoutTool.ToggleGrid) { layoutBox.Cursor = Cursors.Hand; return; }

            if (selectedElement != null && selectedElement.IsSelected)
            {
                RectangleF r = MMToPixelRect(selectedElement.Bounds, dpi);
                ResizeHandle h = CheckHitHandle(mouse, r);
                switch (h)
                {
                    case ResizeHandle.TopLeft: case ResizeHandle.BottomRight: layoutBox.Cursor = Cursors.SizeNWSE; return;
                    case ResizeHandle.TopRight: case ResizeHandle.BottomLeft: layoutBox.Cursor = Cursors.SizeNESW; return;
                    case ResizeHandle.Top: case ResizeHandle.Bottom: layoutBox.Cursor = Cursors.SizeNS; return;
                    case ResizeHandle.Left: case ResizeHandle.Right: layoutBox.Cursor = Cursors.SizeWE; return;
                }
            }
            layoutBox.Cursor = Cursors.Default;
        }

        private string ShowInputBox(string prompt, string title, string defaultText)
        {
            Form promptForm = new Form()
            {
                Width = 400,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = prompt, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340, Text = defaultText };
            Button confirmation = new Button() { Text = "确定", Left = 260, Width = 100, Top = 90, DialogResult = DialogResult.OK };
            promptForm.Controls.Add(textLabel); promptForm.Controls.Add(textBox); promptForm.Controls.Add(confirmation);
            promptForm.AcceptButton = confirmation;
            return promptForm.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        // ==========================================
        // 核心导出逻辑 (关键方法)
        // ==========================================
        public void ExportToImage(string filename, int dpi, ImageFormat format, long quality)
        {
            // 1. 计算目标像素尺寸
            // 像素 = 毫米 / 25.4 * DPI
            float pixelPerMM = dpi / 25.4f;
            int widthPx = (int)(Page.WidthMM * pixelPerMM);
            int heightPx = (int)(Page.HeightMM * pixelPerMM);

            // 2. 创建高清大图
            using (Bitmap bitmap = new Bitmap(widthPx, heightPx))
            {
                // 设置 Bitmap 自身的分辨率元数据
                bitmap.SetResolution(dpi, dpi);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // 3. 设置绘图质量
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // 4. 【核心魔法】调用 Page.Draw
                    // 传入导出的高 DPI，缩放 1.0 (因为我们是画满整张图)，偏移 0
                    Page.Draw(g, dpi, 1.0f, 0, 0);
                }

                // 5. 处理 JPEG 压缩质量
                if (format == ImageFormat.Jpeg)
                {
                    EncoderParameters myEncoderParameters = new EncoderParameters(1);
                    myEncoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                    ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                    bitmap.Save(filename, jpgEncoder, myEncoderParameters);
                }
                else
                {
                    // 其他格式直接保存
                    bitmap.Save(filename, format);
                }
            }
        }

        // 辅助：获取编码器信息
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }
    }
}