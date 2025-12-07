using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using XGIS;

namespace XGIS
{
    // ==========================================
    // 1. 布局元素基类
    // ==========================================
    public abstract class XLayoutElement
    {
        public RectangleF Bounds;
        public bool IsSelected = false;
        public string Name = "Element";
        public bool Visible = true; // 可见性开关

        // 核心绘制方法
        public abstract void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY);

        // 辅助：将毫米转换为屏幕像素
        protected RectangleF MMToPixel(float dpi, float zoom, float offX, float offY)
        {
            float pixelPerMM = dpi / 25.4f;
            float x = Bounds.X * pixelPerMM * zoom + offX;
            float y = Bounds.Y * pixelPerMM * zoom + offY;
            float w = Bounds.Width * pixelPerMM * zoom;
            float h = Bounds.Height * pixelPerMM * zoom;
            return new RectangleF(x, y, w, h);
        }

        protected void DrawSelectionBox(Graphics g, RectangleF rect)
        {
            if (IsSelected)
            {
                // 绘制选中框，稍微外扩一点，以免盖住内容
                using (Pen p = new Pen(Color.Cyan, 2) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(p, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                }
            }
        }
    }

    // ==========================================
    // 2. 地图框
    // ==========================================
    public class XMapFrame : XLayoutElement
    {
        public List<XVectorLayer> Layers;
        public XTileLayer BaseLayer;
        public XView FrameView;
        public bool ShowGrid = false;
        public XExtent StableExtent;

        public XMapFrame(RectangleF bounds, List<XVectorLayer> layers, XTileLayer baseLayer, XExtent currentExtent)
        {
            this.Bounds = bounds;
            this.Name = "地图框"; // 改个中文名方便看
            this.Layers = layers;
            this.BaseLayer = baseLayer;
            // 初始时，稳定范围 = 传入的范围
            this.StableExtent = new XExtent(currentExtent);
            this.FrameView = new XView(new XExtent(currentExtent), new Rectangle(0, 0, 1, 1));
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            if (!Visible) return;

            RectangleF screenRectF = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);
            Rectangle screenRect = new Rectangle((int)screenRectF.X, (int)screenRectF.Y, (int)screenRectF.Width, (int)screenRectF.Height);

            // 【核心修复逻辑】
            // 每次绘制前，强制用 StableExtent 重新计算 View，
            // 这样无论 Frame 怎么忽大忽小，地图都会根据这个稳定的范围重新适配比例尺
            FrameView.Update(new XExtent(StableExtent), screenRect);

            Region oldClip = g.Clip;
            System.Drawing.Drawing2D.Matrix oldTransform = g.Transform;

            // 设置裁剪区域，防止地图画出框外
            g.SetClip(screenRect);
            g.TranslateTransform(screenRect.X, screenRect.Y);

            // 绘制背景
            g.FillRectangle(Brushes.White, 0, 0, screenRect.Width, screenRect.Height);

            // 绘制底图
            if (BaseLayer != null && BaseLayer.Visible) BaseLayer.Draw(g, FrameView);

            // 绘制矢量图层 (倒序，为了和 MapView 保持一致)
            if (Layers != null)
            {
                for (int i = Layers.Count - 1; i >= 0; i--)
                {
                    if (Layers[i].Visible) Layers[i].draw(g, FrameView);
                }
            }

            // 绘制经纬网
            if (ShowGrid) DrawGrid(g, screenRect.Width, screenRect.Height, FrameView);

            // 恢复绘图状态
            g.Transform = oldTransform;
            g.Clip = oldClip;

            // 绘制边框
            g.DrawRectangle(Pens.Black, screenRect);

            // 绘制选中状态
            DrawSelectionBox(g, screenRectF);
        }

        private void DrawGrid(Graphics g, float w, float h, XView view)
        {
            XExtent extent = view.CurrentMapExtent;
            if (extent == null) return;

            int targetTickCount = (int)(w / 100.0);
            if (targetTickCount < 2) targetTickCount = 2;
            double step = XGISMath.CalculateNiceInterval(extent.GetWidth(), targetTickCount);

            double startX = Math.Ceiling(extent.GetMinX() / step) * step;
            double startY = Math.Ceiling(extent.GetMinY() / step) * step;

            using (Pen p = new Pen(Color.Gray, 1) { DashStyle = DashStyle.Dot })
            using (Font f = new Font("Arial", 8))
            using (SolidBrush b = new SolidBrush(Color.Black))
            {
                for (double x = startX; x <= extent.GetMaxX(); x += step)
                {
                    Point pt = view.ToScreenPoint(new XVertex(x, extent.GetMinY()));
                    if (pt.X >= 0 && pt.X <= w)
                    {
                        g.DrawLine(p, pt.X, 0, pt.X, h);
                        g.DrawString(x.ToString("0.##") + "°", f, b, pt.X - 15, h - 15);
                    }
                }
                for (double y = startY; y <= extent.GetMaxY(); y += step)
                {
                    Point pt = view.ToScreenPoint(new XVertex(extent.GetMinX(), y));
                    if (pt.Y >= 0 && pt.Y <= h)
                    {
                        g.DrawLine(p, 0, pt.Y, w, pt.Y);
                        g.DrawString(y.ToString("0.##") + "°", f, b, 2, pt.Y - 6);
                    }
                }
            }
        }
    }

    // ==========================================
    // 3. 指北针
    // ==========================================
    public enum NorthArrowStyle { Simple, Circle, Star }

    public class XNorthArrow : XLayoutElement
    {
        public NorthArrowStyle Style = NorthArrowStyle.Simple;

        public XNorthArrow(RectangleF bounds, NorthArrowStyle style)
        {
            this.Bounds = bounds;
            this.Style = style;
            this.Name = "指北针";
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            if (!Visible) return;

            RectangleF screenRect = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);
            var state = g.Save();

            float centerX = screenRect.X + screenRect.Width / 2;
            float centerY = screenRect.Y + screenRect.Height / 2;
            g.TranslateTransform(centerX, centerY);

            float size = Math.Min(screenRect.Width, screenRect.Height);
            float scale = size / 100f;
            g.ScaleTransform(scale, scale);

            using (Pen p = new Pen(Color.Black, 2))
            using (SolidBrush b = new SolidBrush(Color.Black))
            {
                switch (Style)
                {
                    case NorthArrowStyle.Simple:
                        g.DrawLine(p, 0, 40, 0, -40);
                        g.DrawLine(p, 0, -40, -15, -10);
                        g.DrawLine(p, 0, -40, 15, -10);
                        g.DrawString("N", new Font("Arial", 20, FontStyle.Bold), b, -12, -75);
                        break;
                    case NorthArrowStyle.Circle:
                        g.DrawEllipse(p, -45, -45, 90, 90);
                        g.FillPolygon(b, new PointF[] { new PointF(0, -40), new PointF(15, 10), new PointF(0, 0), new PointF(-15, 10) });
                        g.DrawString("N", new Font("Times New Roman", 16, FontStyle.Bold), b, -10, -70);
                        break;
                    case NorthArrowStyle.Star:
                        PointF[] star = { new PointF(0,-50), new PointF(10,-10), new PointF(50,0), new PointF(10,10),
                                          new PointF(0,50), new PointF(-10,10), new PointF(-50,0), new PointF(-10,-10)};
                        g.FillPolygon(b, star);
                        g.DrawPolygon(Pens.Black, star);
                        g.DrawString("N", new Font("Arial", 14, FontStyle.Bold), b, -10, -75);
                        break;
                }
            }
            g.Restore(state);
            DrawSelectionBox(g, screenRect);
        }
    }

    // ==========================================
    // 4. 比例尺
    // ==========================================
    public enum ScaleBarStyle { Line, AlternatingBar, DoubleLine }

    public class XScaleBar : XLayoutElement
    {
        public ScaleBarStyle Style = ScaleBarStyle.Line;
        public XMapFrame LinkedMapFrame;

        public XScaleBar(RectangleF bounds, ScaleBarStyle style, XMapFrame mapFrame)
        {
            this.Bounds = bounds;
            this.Style = style;
            this.Name = "比例尺";
            this.LinkedMapFrame = mapFrame;
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            if (!Visible) return;

            RectangleF screenRect = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);
            if (LinkedMapFrame == null) return;

            XView view = LinkedMapFrame.FrameView;
            double mapUnitWidth = view.ToMapDistance((int)screenRect.Width);
            XVertex center = view.CurrentMapExtent.GetCenter();
            double metersTotal = XGISMath.GetDistInMeters(center, new XVertex(center.x + mapUnitWidth, center.y));
            double niceDistance = GetNiceRoundNumber(metersTotal);
            float drawWidth = (float)(screenRect.Width * (niceDistance / metersTotal));

            string labelText;
            if (niceDistance >= 1000) labelText = (niceDistance / 1000).ToString("0") + " km";
            else labelText = niceDistance.ToString("0") + " m";

            using (Pen p = new Pen(Color.Black, 2))
            using (SolidBrush bBlack = new SolidBrush(Color.Black))
            using (SolidBrush bWhite = new SolidBrush(Color.White))
            using (Font f = new Font("Arial", 10 * zoomScale))
            {
                float x = screenRect.X;
                float y = screenRect.Y;
                float h = screenRect.Height;

                switch (Style)
                {
                    case ScaleBarStyle.Line:
                        float midY = y + h / 2;
                        g.DrawLine(p, x, midY, x + drawWidth, midY);
                        g.DrawLine(p, x, midY, x, midY - 5);
                        g.DrawLine(p, x + drawWidth, midY, x + drawWidth, midY - 5);
                        g.DrawLine(p, x + drawWidth / 2, midY, x + drawWidth / 2, midY - 5);
                        g.DrawString("0", f, bBlack, x - 5, midY + 2);
                        g.DrawString(labelText, f, bBlack, x + drawWidth - 20, midY + 2);
                        break;

                    case ScaleBarStyle.AlternatingBar:
                        float barH = h / 3;
                        float segW = drawWidth / 4;
                        g.FillRectangle(bBlack, x, y + barH, segW, barH);
                        g.FillRectangle(bWhite, x + segW, y + barH, segW, barH); g.DrawRectangle(p, x + segW, y + barH, segW, barH);
                        g.FillRectangle(bBlack, x + 2 * segW, y + barH, segW, barH);
                        g.FillRectangle(bWhite, x + 3 * segW, y + barH, segW, barH); g.DrawRectangle(p, x + 3 * segW, y + barH, segW, barH);
                        g.DrawString("0", f, bBlack, x, y + barH * 2);
                        g.DrawString(labelText, f, bBlack, x + drawWidth - 20, y + barH * 2);
                        break;

                    case ScaleBarStyle.DoubleLine:
                        float dY = y + h / 2;
                        g.DrawRectangle(p, x, dY - 3, drawWidth, 6);
                        g.FillRectangle(bBlack, x, dY - 3, drawWidth / 2, 6);
                        g.DrawString("0", f, bBlack, x, dY - 20);
                        g.DrawString(labelText, f, bBlack, x + drawWidth - 20, dY - 20);
                        break;
                }
            }
            DrawSelectionBox(g, screenRect);
        }

        private double GetNiceRoundNumber(double val)
        {
            if (val == 0) return 0;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(val)));
            double normalized = val / magnitude;
            if (normalized >= 5) return 5 * magnitude;
            if (normalized >= 2) return 2 * magnitude;
            return 1 * magnitude;
        }
    }

    // ==========================================
    // 5. 文本元素
    // ==========================================
    public class XTextElement : XLayoutElement
    {
        public string Text = "文本";
        public Font Font = new Font("宋体", 12); // 默认宋体
        public Color Color = Color.Black;

        // 新增描边属性
        public bool UseOutline = false;
        public Color OutlineColor = Color.White;
        public float OutlineWidth = 2.0f;

        public XTextElement(RectangleF bounds, string text)
        {
            this.Bounds = bounds;
            this.Text = text;
            this.Name = "文本框";
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            if (!Visible) return;
            RectangleF screenRect = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);

            // 使用 GraphicsPath 实现高质量描边文字
            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                // 计算字体在屏幕上的实际像素大小
                float emSize = g.DpiY * Font.SizeInPoints / 72 * zoomScale;

                // 将文字添加到路径
                path.AddString(
                    Text,
                    Font.FontFamily,
                    (int)Font.Style,
                    emSize,
                    screenRect.Location, // 位置
                    StringFormat.GenericDefault);

                // 1. 如果开启描边，先画描边 (画在底下)
                if (UseOutline)
                {
                    using (Pen p = new Pen(OutlineColor, OutlineWidth * zoomScale))
                    {
                        p.LineJoin = System.Drawing.Drawing2D.LineJoin.Round; // 圆角连接，防止尖刺
                        g.DrawPath(p, path);
                    }
                }

                // 2. 填充文字颜色
                using (SolidBrush b = new SolidBrush(Color))
                {
                    g.FillPath(b, path);
                }
            }

            DrawSelectionBox(g, screenRect);
        }
    }

    // ==========================================
    // 6. 图例元素 (新增)
    // ==========================================
    // ==========================================
    // 6. 图例元素 (修复版：支持高 DPI 导出)
    // ==========================================
    public class LayoutLegend : XLayoutElement
    {
        public XMapFrame LinkedMapFrame;

        public LayoutLegend(XMapFrame mapFrame)
        {
            this.LinkedMapFrame = mapFrame;
            this.Name = "图例";
            this.Bounds = new RectangleF(10, 10, 50, 80); // 默认大小 (毫米)
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            if (!Visible) return;
            if (LinkedMapFrame == null || LinkedMapFrame.Layers == null) return;

            RectangleF screenRect = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);

            // 1. 计算当前环境下的 1毫米 等于多少像素
            float pixelPerMM = screenDpi / 25.4f;

            // 2. 【核心修复】所有的间距、大小都用 MM 定义，然后转为像素
            // 这样 300 DPI 下，行高也会自动变大 3 倍，就不会挤了
            float padding = 2.0f * pixelPerMM * zoomScale;      // 内边距 2mm
            float rowHeight = 7.0f * pixelPerMM * zoomScale;    // 行高 7mm
            float titleHeight = 10.0f * pixelPerMM * zoomScale; // 标题行高 10mm
            float patchWidth = 8.0f * pixelPerMM * zoomScale;   // 符号宽 8mm
            float patchHeight = 4.0f * pixelPerMM * zoomScale;  // 符号高 4mm
            float gap = 3.0f * pixelPerMM * zoomScale;          // 文字和符号的间距 3mm

            // 绘制背景框
            using (SolidBrush bgBrush = new SolidBrush(Color.White))
            using (Pen borderPen = new Pen(Color.Black, 1)) // 边框线宽也可以 * zoomScale
            {
                g.FillRectangle(bgBrush, screenRect);
                g.DrawRectangle(borderPen, screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
            }

            float currentX = screenRect.X + padding;
            float currentY = screenRect.Y + padding;

            // 3. 绘制 "图例" 标题
            // 字体大小保持不变，GDI+ 会自动处理 DPI 缩放
            using (Font titleFont = new Font("微软雅黑", 10 * zoomScale, FontStyle.Bold))
            using (Font itemFont = new Font("微软雅黑", 9 * zoomScale))
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                // 绘制标题
                // 稍微往下移一点居中
                g.DrawString("图例", titleFont, textBrush, currentX, currentY);
                currentY += titleHeight;

                // 4. 遍历图层 (倒序)
                for (int i = LinkedMapFrame.Layers.Count - 1; i >= 0; i--)
                {
                    XVectorLayer layer = LinkedMapFrame.Layers[i];
                    if (!layer.Visible) continue;

                    // 绘制图层名称
                    g.DrawString(layer.Name, itemFont, textBrush, currentX, currentY);
                    currentY += rowHeight;

                    // 绘制符号和子项
                    if (layer.ThematicMode == RenderMode.SingleSymbol)
                    {
                        DrawItem(g, layer, layer.UnselectedThematic, "所有要素", itemFont, textBrush,
                                 currentX, currentY, patchWidth, patchHeight, gap);
                        currentY += rowHeight;
                    }
                    else if (layer.ThematicMode == RenderMode.UniqueValues)
                    {
                        foreach (var kvp in layer.UniqueRenderer)
                        {
                            DrawItem(g, layer, kvp.Value, kvp.Key, itemFont, textBrush,
                                     currentX, currentY, patchWidth, patchHeight, gap);
                            currentY += rowHeight;
                        }
                    }
                    else if (layer.ThematicMode == RenderMode.GraduatedSymbols)
                    {
                        foreach (var cb in layer.ClassBreaks)
                        {
                            DrawItem(g, layer, cb.Thematic, cb.Label, itemFont, textBrush,
                                     currentX, currentY, patchWidth, patchHeight, gap);
                            currentY += rowHeight;
                        }
                    }

                    currentY += padding; // 图层之间多空一点

                    // 防溢出 (可选)
                    if (currentY > screenRect.Bottom) break;
                }
            }

            DrawSelectionBox(g, screenRect);
        }

        // 辅助：绘制单行图例项 (符号 + 文字)
        private void DrawItem(Graphics g, XVectorLayer layer, XThematic th, string label,
                              Font font, Brush brush,
                              float x, float y, float w, float h, float gap)
        {
            // 1. 画符号
            // 计算符号垂直居中
            // 当前行高约等于 h * 1.8 左右 (根据 rowHeight 设定的)
            // 我们简单让它向下偏移一点
            float symbolY = y + h * 0.2f;

            DrawPatch(g, layer, th, x, symbolY, w, h);

            // 2. 画文字
            // 文字在符号右边
            g.DrawString(label, font, brush, x + w + gap, y);
        }

        private void DrawPatch(Graphics g, XVectorLayer layer, XThematic th, float x, float y, float w, float h)
        {
            if (th == null) return;
            float midY = y + h / 2;

            if (layer.ShapeType == SHAPETYPE.polygon)
            {
                g.FillRectangle(th.PolygonBrush, x, y, w, h);
                g.DrawRectangle(th.PolygonPen, x, y, w, h);
            }
            else if (layer.ShapeType == SHAPETYPE.line)
            {
                g.DrawLine(th.LinePen, x, midY, x + w, midY);
            }
            else if (layer.ShapeType == SHAPETYPE.point)
            {
                float r = Math.Min(w, h) / 2;
                float cx = x + w / 2;
                g.FillEllipse(th.PointBrush, cx - r, midY - r, 2 * r, 2 * r);
                g.DrawEllipse(th.PointPen, cx - r, midY - r, 2 * r, 2 * r);
            }
        }
    }

    // 辅助数学类
    public static class XGISMath
    {
        public static double CalculateNiceInterval(double range, int targetTickCount)
        {
            double roughStep = range / targetTickCount;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
            double normalizedStep = roughStep / magnitude;
            double niceStep;
            if (normalizedStep < 1.5) niceStep = 1;
            else if (normalizedStep < 3) niceStep = 2;
            else if (normalizedStep < 7) niceStep = 5;
            else niceStep = 10;
            return niceStep * magnitude;
        }

        public static double GetDistInMeters(XVertex p1, XVertex p2)
        {
            double avgLat = (p1.y + p2.y) / 2.0;
            double radLat = avgLat * Math.PI / 180.0;
            double dx = Math.Abs(p1.x - p2.x);
            double dy = Math.Abs(p1.y - p2.y);
            double lx = dx * 111320 * Math.Cos(radLat);
            double ly = dy * 110574;
            return Math.Sqrt(lx * lx + ly * ly);
        }
    }

    public class XLayoutPage
    {
        public float WidthMM = 297;
        public float HeightMM = 210;
        public List<XLayoutElement> Elements = new List<XLayoutElement>();

        public void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            float pixelPerMM = screenDpi / 25.4f;
            float w = WidthMM * pixelPerMM * zoomScale;
            float h = HeightMM * pixelPerMM * zoomScale;

            // 画纸张阴影和背景
            g.FillRectangle(Brushes.Gray, offsetX + 5, offsetY + 5, w, h);
            g.FillRectangle(Brushes.White, offsetX, offsetY, w, h);
            g.DrawRectangle(Pens.Black, offsetX, offsetY, w, h);

            // 绘制所有元素 (顺序：index 0 在最下，index count-1 在最上)
            foreach (var ele in Elements)
            {
                ele.Draw(g, screenDpi, zoomScale, offsetX, offsetY);
            }
        }
    }
}