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
        None, PanPaper, Select, CreateMapFrame, ResizeElement, PanMapContent,
        CreateNorthArrow, CreateScaleBar, ToggleGrid
    }

    public enum ResizeHandle { None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }

    public partial class LayoutControl : UserControl
    {
        // 公开 Page 数据
        public XLayoutPage Page;

        // 事件：当元素列表发生变化（添加、删除、重排序）时触发
        public event EventHandler ElementChanged;
        // 事件：当选中状态发生变化时触发 (用于同步 TreeView 高亮)
        public event EventHandler SelectionChanged;

        private float zoomScale = 1.0f;
        private float offsetX = 40;
        private float offsetY = 40;

        public LayoutTool CurrentTool = LayoutTool.Select;
        private XLayoutElement selectedElement = null;
        public XLayoutElement SelectedElement => selectedElement; // 只读属性供外部查询

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
            this.Page = new XLayoutPage();

            InitContextMenu();
            layoutBox.MouseWheel += LayoutBox_MouseWheel;
            layoutBox.MouseDown += LayoutBox_MouseDown;
            layoutBox.MouseMove += LayoutBox_MouseMove;
            layoutBox.MouseUp += LayoutBox_MouseUp;
            layoutBox.Paint += LayoutBox_Paint;
            this.KeyDown += LayoutControl_KeyDown;
        }

        private void NotifyElementChanged()
        {
            ElementChanged?.Invoke(this, EventArgs.Empty);
        }

        private void NotifySelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        // 提供给 Form1 调用：从外部设置选中（同步 TreeView 点击）
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
            // 更新所有现有的地图框数据
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

        // ================= 工具启动 =================
        public void StartCreateMapFrame() { CurrentTool = LayoutTool.CreateMapFrame; layoutBox.Cursor = Cursors.Cross; DeselectAll(); }
        public void StartCreateNorthArrow(NorthArrowStyle style) { CurrentTool = LayoutTool.CreateNorthArrow; _pendingNorthArrowStyle = style; layoutBox.Cursor = Cursors.Cross; DeselectAll(); }
        public void StartCreateScaleBar(ScaleBarStyle style) { CurrentTool = LayoutTool.CreateScaleBar; _pendingScaleBarStyle = style; layoutBox.Cursor = Cursors.Cross; DeselectAll(); }
        public void StartToggleGrid() { CurrentTool = LayoutTool.ToggleGrid; layoutBox.Cursor = Cursors.Hand; DeselectAll(); }

        private void DeselectAll()
        {
            selectedElement = null;
            activeMapFrame = null;
            foreach (var ele in Page.Elements) ele.IsSelected = false;
            layoutBox.Invalidate();
            NotifySelectionChanged();
        }

        // ================= 键盘删除 =================
        private void LayoutControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && selectedElement != null && activeMapFrame == null)
            {
                Page.Elements.Remove(selectedElement);
                selectedElement = null;
                layoutBox.Invalidate();
                NotifyElementChanged(); // 通知 Form1 更新树
            }
        }

        // ================= 鼠标事件 =================
        private void LayoutBox_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();
            mouseDownLoc = e.Location;
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) { dpi = g.DpiX; }
            float pixelPerMM = dpi / 25.4f;

            if (activeMapFrame != null) { if (e.Button == MouseButtons.Right) contextMenuLayout.Show(layoutBox, e.Location); return; }
            if (CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow || CurrentTool == LayoutTool.CreateScaleBar || CurrentTool == LayoutTool.ToggleGrid) return;

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
                    if (currentHandle != ResizeHandle.None) { CurrentTool = LayoutTool.ResizeElement; originalBounds = selectedElement.Bounds; return; }
                }

                if (CheckHitElement(e.Location, pixelPerMM))
                {
                    CurrentTool = LayoutTool.Select;
                    originalBounds = selectedElement.Bounds;
                    NotifySelectionChanged(); // 通知 Form1 选中树节点
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
            else if (e.Button == MouseButtons.Middle) { CurrentTool = LayoutTool.PanPaper; }
        }

        private void LayoutBox_MouseMove(object sender, MouseEventArgs e)
        {
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) { dpi = g.DpiX; }
            float pixelPerMM = dpi / 25.4f;

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

            if ((CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow || CurrentTool == LayoutTool.CreateScaleBar) && e.Button == MouseButtons.Left)
            {
                layoutBox.Invalidate();
                return;
            }

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
                if (newBounds.Width < 5) newBounds.Width = 5; if (newBounds.Height < 5) newBounds.Height = 5;
                selectedElement.Bounds = newBounds;
                layoutBox.Invalidate();
            }
            else if (CurrentTool == LayoutTool.Select && e.Button == MouseButtons.Left && selectedElement != null)
            {
                float dx = (e.X - mouseDownLoc.X) / zoomScale / pixelPerMM;
                float dy = (e.Y - mouseDownLoc.Y) / zoomScale / pixelPerMM;
                selectedElement.Bounds = new RectangleF(originalBounds.X + dx, originalBounds.Y + dy, originalBounds.Width, originalBounds.Height);
                layoutBox.Invalidate();
            }
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
            Rectangle screenRect = GetScreenRectFromPoints(mouseDownLoc, e.Location);

            Action<XLayoutElement> FinishCreation = (newEle) => {
                Page.Elements.Add(newEle);
                selectedElement = newEle;
                selectedElement.IsSelected = true;
                CurrentTool = LayoutTool.Select;
                layoutBox.Cursor = Cursors.Default;
                layoutBox.Invalidate();
                NotifyElementChanged(); // 通知树结构变化
                NotifySelectionChanged(); // 通知选中变化
            };

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
            else if (CurrentTool == LayoutTool.CreateNorthArrow && e.Button == MouseButtons.Left)
            {
                RectangleF mmRect;
                if (screenRect.Width < 5) { RectangleF p = PixelToMMRect(new Rectangle(e.X, e.Y, 0, 0), dpi); mmRect = new RectangleF(p.X, p.Y, 20, 20); }
                else mmRect = PixelToMMRect(screenRect, dpi);
                FinishCreation(new XNorthArrow(mmRect, _pendingNorthArrowStyle));
                return;
            }
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
            Page.Draw(e.Graphics, dpi, zoomScale, offsetX, offsetY);

            if (activeMapFrame == null && selectedElement != null && selectedElement.IsSelected) DrawSelectionHandles(e.Graphics, dpi);
            if (activeMapFrame != null)
            {
                RectangleF rect = MMToPixelRect(activeMapFrame.Bounds, dpi);
                using (Pen p = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash }) e.Graphics.DrawRectangle(p, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
            }
            bool isCreating = CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow || CurrentTool == LayoutTool.CreateScaleBar;
            if (isCreating && mouseDownLoc != Point.Empty && Control.MouseButtons == MouseButtons.Left)
            {
                Point cur = layoutBox.PointToClient(Cursor.Position);
                Rectangle rect = GetScreenRectFromPoints(mouseDownLoc, cur);
                using (Pen p = new Pen(Color.Blue, 1) { DashStyle = DashStyle.Dot }) e.Graphics.DrawRectangle(p, rect);
            }
            DrawRulers(e.Graphics, dpi, zoomScale, offsetX, offsetY);
        }

        // ================= 辅助函数区 (保持精简) =================
        private void LayoutBox_MouseWheel(object sender, MouseEventArgs e)
        {
            float dpi = 96; using (Graphics g = layoutBox.CreateGraphics()) { dpi = g.DpiX; }
            if (activeMapFrame != null)
            {
                bool isZoomIn = e.Delta > 0;
                XVertex center = activeMapFrame.FrameView.ToMapVertex(e.Location);
                activeMapFrame.FrameView.CurrentMapExtent.ZoomToCenter(center, isZoomIn ? 1.1 : 0.9);
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
            RectangleF r = MMToPixelRect(selectedElement.Bounds, dpi);
            int s = 6;
            PointF[] pts = GetHandlePoints(r);
            foreach (PointF p in pts) { g.FillRectangle(Brushes.White, p.X - s / 2, p.Y - s / 2, s, s); g.DrawRectangle(Pens.Blue, p.X - s / 2, p.Y - s / 2, s, s); }
        }

        private PointF[] GetHandlePoints(RectangleF rect)
        {
            return new PointF[] { new PointF(rect.Left, rect.Top), new PointF(rect.Left + rect.Width / 2, rect.Top), new PointF(rect.Right, rect.Top), new PointF(rect.Right, rect.Top + rect.Height / 2), new PointF(rect.Right, rect.Bottom), new PointF(rect.Left + rect.Width / 2, rect.Bottom), new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top + rect.Height / 2) };
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
            // 倒序检测，优先选中最上面的
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
            if (new RectangleF(rect.Right - s, rect.Bottom - s, s * 2, s * 2).Contains(mouse)) return ResizeHandle.BottomRight;
            if (new RectangleF(rect.Right - s, rect.Top, s * 2, rect.Height).Contains(mouse)) return ResizeHandle.Right;
            if (new RectangleF(rect.Left, rect.Bottom - s, rect.Width, s * 2).Contains(mouse)) return ResizeHandle.Bottom;
            return ResizeHandle.None;
        }

        private void UpdateCursor(Point mouse, float dpi)
        {
            if (activeMapFrame != null) { layoutBox.Cursor = Cursors.NoMove2D; return; }
            if (CurrentTool == LayoutTool.CreateMapFrame || CurrentTool == LayoutTool.CreateNorthArrow || CurrentTool == LayoutTool.CreateScaleBar) { layoutBox.Cursor = Cursors.Cross; return; }
            if (CurrentTool == LayoutTool.ToggleGrid) { layoutBox.Cursor = Cursors.Hand; return; }
            if (selectedElement != null && selectedElement.IsSelected)
            {
                if (CheckHitHandle(mouse, MMToPixelRect(selectedElement.Bounds, dpi)) == ResizeHandle.BottomRight) { layoutBox.Cursor = Cursors.SizeNWSE; return; }
            }
            layoutBox.Cursor = Cursors.Default;
        }
    }
}