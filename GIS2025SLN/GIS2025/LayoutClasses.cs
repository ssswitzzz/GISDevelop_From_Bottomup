using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using XGIS;

namespace XGIS
{
    public abstract class XLayoutElement
    {
        public RectangleF Bounds;
        public bool IsSelected = false;
        public string Name = "Element";
        public bool Visible = true;

        public abstract void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY);

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
                using (Pen p = new Pen(Color.Cyan, 2) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(p, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                }
            }
        }
    }

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
            this.Name = "地图框";
            this.Layers = layers;
            this.BaseLayer = baseLayer;
            this.StableExtent = new XExtent(currentExtent);
            this.FrameView = new XView(new XExtent(currentExtent), new Rectangle(0, 0, 1, 1));
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            if (!Visible) return;

            RectangleF screenRectF = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);
            Rectangle screenRect = new Rectangle((int)screenRectF.X, (int)screenRectF.Y, (int)screenRectF.Width, (int)screenRectF.Height);

            FrameView.Update(new XExtent(StableExtent), screenRect);

            Region oldClip = g.Clip;
            System.Drawing.Drawing2D.Matrix oldTransform = g.Transform;

            g.SetClip(screenRect);
            g.TranslateTransform(screenRect.X, screenRect.Y);
            g.FillRectangle(Brushes.White, 0, 0, screenRect.Width, screenRect.Height);

            if (BaseLayer != null && BaseLayer.Visible) BaseLayer.Draw(g, FrameView);

            // 计算 DPI 缩放比例
            float dpiScale = screenDpi / 96.0f;

            if (Layers != null)
            {
                for (int i = Layers.Count - 1; i >= 0; i--)
                {
                    if (Layers[i].Visible) Layers[i].draw(g, FrameView, dpiScale);
                }
            }

            if (ShowGrid) DrawGrid(g, screenRect.Width, screenRect.Height, FrameView, dpiScale);

            g.Transform = oldTransform;
            g.Clip = oldClip;

            // 边框加粗
            using (Pen borderPen = new Pen(Color.Black, 1 * dpiScale))
            {
                g.DrawRectangle(borderPen, screenRect);
            }

            DrawSelectionBox(g, screenRectF);
        }

        private void DrawGrid(Graphics g, float w, float h, XView view, float dpiScale)
        {
            if (view.CurrentMapExtent == null) return;
            XExtent extent = view.CurrentMapExtent;

            int targetTickCount = (int)(w / (100.0 * dpiScale));
            if (targetTickCount < 2) targetTickCount = 2;
            double step = XGISMath.CalculateNiceInterval(extent.GetWidth(), targetTickCount);

            double startX = Math.Ceiling(extent.GetMinX() / step) * step;
            double startY = Math.Ceiling(extent.GetMinY() / step) * step;

            using (Pen p = new Pen(Color.Gray, 1 * dpiScale) { DashStyle = DashStyle.Dot })
            using (Font f = new Font("Arial", 8))
            using (SolidBrush b = new SolidBrush(Color.Black))
            {
                float textOffX = 15 * dpiScale;
                float textOffY = 6 * dpiScale;

                for (double x = startX; x <= extent.GetMaxX(); x += step)
                {
                    Point pt = view.ToScreenPoint(new XVertex(x, extent.GetMinY()));
                    if (pt.X >= 0 && pt.X <= w)
                    {
                        g.DrawLine(p, pt.X, 0, pt.X, h);
                        g.DrawString(x.ToString("0.##") + "°", f, b, pt.X - textOffX, h - textOffX);
                    }
                }
                for (double y = startY; y <= extent.GetMaxY(); y += step)
                {
                    Point pt = view.ToScreenPoint(new XVertex(extent.GetMinX(), y));
                    if (pt.Y >= 0 && pt.Y <= h)
                    {
                        g.DrawLine(p, 0, pt.Y, w, pt.Y);
                        g.DrawString(y.ToString("0.##") + "°", f, b, 2 * dpiScale, pt.Y - textOffY);
                    }
                }
            }
        }
    }

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
            using (Pen p = new Pen(Color.Black, 2f))
            using (SolidBrush b = new SolidBrush(Color.Black))
            {
                switch (Style)
                {
                    case NorthArrowStyle.Simple:
                        g.DrawLine(p, 0, 40, 0, -40);
                        g.DrawLine(p, 0, -40, -15, -10);
                        g.DrawLine(p, 0, -40, 15, -10);
                        using (Font f = new Font("Arial", 24, FontStyle.Bold, GraphicsUnit.Pixel))
                        {
                            g.DrawString("N", f, b, -8, -75);
                        }
                        break;

                    case NorthArrowStyle.Circle:
                        g.DrawEllipse(p, -45, -45, 90, 90);
                        g.FillPolygon(b, new PointF[] { new PointF(0, -40), new PointF(15, 10), new PointF(0, 0), new PointF(-15, 10) });
                        using (Font f = new Font("Times New Roman", 20, FontStyle.Bold, GraphicsUnit.Pixel))
                        {
                            g.DrawString("N", f, b, -7, -70);
                        }
                        break;

                    case NorthArrowStyle.Star:
                        PointF[] star = { new PointF(0,-50), new PointF(10,-10), new PointF(50,0), new PointF(10,10),
                                          new PointF(0,50), new PointF(-10,10), new PointF(-50,0), new PointF(-10,-10)};
                        g.FillPolygon(b, star);
                        g.DrawPolygon(p, star);
                        using (Font f = new Font("Arial", 18, FontStyle.Bold, GraphicsUnit.Pixel))
                        {
                            g.DrawString("N", f, b, -6, -75);
                        }
                        break;
                }
            }
            g.Restore(state);
            DrawSelectionBox(g, screenRect);
        }
    }

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

            // 比例尺没有使用 ScaleTransform，所以这里必须乘 dpiScale
            float dpiScale = screenDpi / 96.0f;

            XView view = LinkedMapFrame.FrameView;
            double mapUnitWidth = view.ToMapDistance((int)screenRect.Width);
            XVertex center = view.CurrentMapExtent.GetCenter();
            double metersTotal = XGISMath.GetDistInMeters(center, new XVertex(center.x + mapUnitWidth, center.y));
            double niceDistance = GetNiceRoundNumber(metersTotal);
            float drawWidth = (float)(screenRect.Width * (niceDistance / metersTotal));

            string labelText;
            if (niceDistance >= 1000) labelText = (niceDistance / 1000).ToString("0") + " km";
            else labelText = niceDistance.ToString("0") + " m";

            using (Pen p = new Pen(Color.Black, 2 * dpiScale))
            using (SolidBrush bBlack = new SolidBrush(Color.Black))
            using (SolidBrush bWhite = new SolidBrush(Color.White))
            // 字体大小不需要乘 dpiScale，GDI+ 会自动处理 Points 单位
            using (Font f = new Font("Arial", 10 * zoomScale))
            {
                float x = screenRect.X;
                float y = screenRect.Y;
                float h = screenRect.Height;

                float tickH = 5 * dpiScale * zoomScale;
                float textOffset = 2 * dpiScale * zoomScale;
                float labelPadding = 20 * dpiScale * zoomScale;
                float midY = y + h / 2;

                switch (Style)
                {
                    case ScaleBarStyle.Line:
                        g.DrawLine(p, x, midY, x + drawWidth, midY);
                        g.DrawLine(p, x, midY, x, midY - tickH);
                        g.DrawLine(p, x + drawWidth, midY, x + drawWidth, midY - tickH);
                        g.DrawLine(p, x + drawWidth / 2, midY, x + drawWidth / 2, midY - tickH);
                        g.DrawString("0", f, bBlack, x - tickH, midY + textOffset);
                        g.DrawString(labelText, f, bBlack, x + drawWidth - labelPadding, midY + textOffset);
                        break;

                    case ScaleBarStyle.AlternatingBar:
                        float barH = h / 3;
                        float segW = drawWidth / 4;
                        g.FillRectangle(bBlack, x, y + barH, segW, barH); g.DrawRectangle(p, x, y + barH, segW, barH);
                        g.FillRectangle(bWhite, x + segW, y + barH, segW, barH); g.DrawRectangle(p, x + segW, y + barH, segW, barH);
                        g.FillRectangle(bBlack, x + 2 * segW, y + barH, segW, barH); g.DrawRectangle(p, x + 2 * segW, y + barH, segW, barH);
                        g.FillRectangle(bWhite, x + 3 * segW, y + barH, segW, barH); g.DrawRectangle(p, x + 3 * segW, y + barH, segW, barH);
                        g.DrawString("0", f, bBlack, x, y + barH * 2);
                        g.DrawString(labelText, f, bBlack, x + drawWidth - labelPadding, y + barH * 2);
                        break;

                    case ScaleBarStyle.DoubleLine:
                        float dY = y + h / 2;
                        float barH2 = 6 * dpiScale * zoomScale;
                        g.DrawRectangle(p, x, dY - barH2 / 2, drawWidth, barH2);
                        g.FillRectangle(bBlack, x, dY - barH2 / 2, drawWidth / 2, barH2);
                        g.DrawString("0", f, bBlack, x, dY - labelPadding);
                        g.DrawString(labelText, f, bBlack, x + drawWidth - labelPadding, dY - labelPadding);
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

    public class XTextElement : XLayoutElement
    {
        public string Text = "文本";
        public Font Font = new Font("宋体", 12);
        public Color Color = Color.Black;
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
            float dpiScale = screenDpi / 96.0f;

            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                float emSize = g.DpiY * Font.SizeInPoints / 72 * zoomScale;

                path.AddString(Text, Font.FontFamily, (int)Font.Style, emSize, screenRect.Location, StringFormat.GenericDefault);

                if (UseOutline)
                {
                    using (Pen p = new Pen(OutlineColor, OutlineWidth * zoomScale * dpiScale))
                    {
                        p.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                        g.DrawPath(p, path);
                    }
                }

                using (SolidBrush b = new SolidBrush(Color))
                {
                    g.FillPath(b, path);
                }
            }
            DrawSelectionBox(g, screenRect);
        }
    }

    public class LayoutLegend : XLayoutElement
    {
        public XMapFrame LinkedMapFrame;

        public LayoutLegend(XMapFrame mapFrame)
        {
            this.LinkedMapFrame = mapFrame;
            this.Name = "图例";
            this.Bounds = new RectangleF(10, 10, 50, 80);
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            if (!Visible || LinkedMapFrame == null || LinkedMapFrame.Layers == null) return;

            RectangleF screenRect = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);

            float pixelPerMM = screenDpi / 25.4f;
            float padding = 2.0f * pixelPerMM * zoomScale;
            float rowHeight = 7.0f * pixelPerMM * zoomScale;
            float titleHeight = 10.0f * pixelPerMM * zoomScale;
            float patchWidth = 8.0f * pixelPerMM * zoomScale;
            float patchHeight = 4.0f * pixelPerMM * zoomScale;
            float gap = 3.0f * pixelPerMM * zoomScale;

            using (SolidBrush bgBrush = new SolidBrush(Color.White))
            using (Pen borderPen = new Pen(Color.Black, 1 * (screenDpi / 96.0f)))
            {
                g.FillRectangle(bgBrush, screenRect);
                g.DrawRectangle(borderPen, screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
            }

            float currentX = screenRect.X + padding;
            float currentY = screenRect.Y + padding;

            using (Font titleFont = new Font("微软雅黑", 10 * zoomScale, FontStyle.Bold))
            using (Font itemFont = new Font("微软雅黑", 9 * zoomScale))
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                g.DrawString("图例", titleFont, textBrush, currentX, currentY);
                currentY += titleHeight;

                for (int i = LinkedMapFrame.Layers.Count - 1; i >= 0; i--)
                {
                    XVectorLayer layer = LinkedMapFrame.Layers[i];
                    if (!layer.Visible) continue;

                    g.DrawString(layer.Name, itemFont, textBrush, currentX, currentY);
                    currentY += rowHeight;

                    if (layer.ThematicMode == RenderMode.SingleSymbol)
                    {
                        DrawItem(g, layer, layer.UnselectedThematic, "所有要素", itemFont, textBrush,
                                 currentX, currentY, patchWidth, patchHeight, gap, screenDpi);
                        currentY += rowHeight;
                    }
                    else if (layer.ThematicMode == RenderMode.UniqueValues)
                    {
                        foreach (var kvp in layer.UniqueRenderer)
                        {
                            DrawItem(g, layer, kvp.Value, kvp.Key, itemFont, textBrush,
                                     currentX, currentY, patchWidth, patchHeight, gap, screenDpi);
                            currentY += rowHeight;
                        }
                    }
                    else if (layer.ThematicMode == RenderMode.GraduatedSymbols)
                    {
                        foreach (var cb in layer.ClassBreaks)
                        {
                            DrawItem(g, layer, cb.Thematic, cb.Label, itemFont, textBrush,
                                     currentX, currentY, patchWidth, patchHeight, gap, screenDpi);
                            currentY += rowHeight;
                        }
                    }

                    currentY += padding;
                    if (currentY > screenRect.Bottom) break;
                }
            }
            DrawSelectionBox(g, screenRect);
        }

        private void DrawItem(Graphics g, XVectorLayer layer, XThematic th, string label,
                              Font font, Brush brush,
                              float x, float y, float w, float h, float gap, float dpi)
        {
            float symbolY = y + h * 0.2f;
            DrawPatch(g, layer, th, x, symbolY, w, h, dpi);
            g.DrawString(label, font, brush, x + w + gap, y);
        }

        private void DrawPatch(Graphics g, XVectorLayer layer, XThematic th, float x, float y, float w, float h, float dpi)
        {
            if (th == null) return;
            float midY = y + h / 2;
            float dpiScale = dpi / 96.0f;

            if (layer.ShapeType == SHAPETYPE.polygon)
            {
                g.FillRectangle(th.PolygonBrush, x, y, w, h);
                using (Pen p = new Pen(th.PolygonPen.Color, th.PolygonPen.Width * dpiScale))
                    g.DrawRectangle(p, x, y, w, h);
            }
            else if (layer.ShapeType == SHAPETYPE.line)
            {
                using (Pen p = new Pen(th.LinePen.Color, th.LinePen.Width * dpiScale))
                    g.DrawLine(p, x, midY, x + w, midY);
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

            g.FillRectangle(Brushes.Gray, offsetX + 5, offsetY + 5, w, h);
            g.FillRectangle(Brushes.White, offsetX, offsetY, w, h);
            g.DrawRectangle(Pens.Black, offsetX, offsetY, w, h);

            foreach (var ele in Elements)
            {
                ele.Draw(g, screenDpi, zoomScale, offsetX, offsetY);
            }
        }
    }
}