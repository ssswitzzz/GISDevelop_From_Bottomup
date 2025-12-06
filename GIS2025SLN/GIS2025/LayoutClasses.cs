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

        // 辅助：绘制选中状态的虚线框
        protected void DrawSelectionBox(Graphics g, RectangleF rect)
        {
            if (IsSelected)
            {
                using (Pen p = new Pen(Color.Cyan, 2))
                {
                    p.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(p, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }
    }

    // ==========================================
    // 2. 地图框 (支持经纬网)
    // ==========================================
    public class XMapFrame : XLayoutElement
    {
        public List<XVectorLayer> Layers;
        public XTileLayer BaseLayer;
        public XView FrameView;
        public bool ShowGrid = false; // 网格开关

        public XMapFrame(RectangleF bounds, List<XVectorLayer> layers, XTileLayer baseLayer, XExtent currentExtent)
        {
            this.Bounds = bounds;
            this.Name = "Map Frame";
            this.Layers = layers;
            this.BaseLayer = baseLayer;
            this.FrameView = new XView(new XExtent(currentExtent), new Rectangle(0, 0, 1, 1));
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            RectangleF screenRectF = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);
            Rectangle screenRect = new Rectangle((int)screenRectF.X, (int)screenRectF.Y, (int)screenRectF.Width, (int)screenRectF.Height);

            // 更新视图窗口大小
            FrameView.UpdateMapWindow(new Rectangle(0, 0, screenRect.Width, screenRect.Height));

            // 保存状态
            Region oldClip = g.Clip;
            System.Drawing.Drawing2D.Matrix oldTransform = g.Transform;

            // 设置裁剪和平移
            g.SetClip(screenRect);
            g.TranslateTransform(screenRect.X, screenRect.Y);
            g.FillRectangle(Brushes.White, 0, 0, screenRect.Width, screenRect.Height);

            // 绘制底图
            if (BaseLayer != null && BaseLayer.Visible)
            {
                BaseLayer.Draw(g, FrameView);
            }

            // 绘制矢量图层
            if (Layers != null)
            {
                for (int i = Layers.Count - 1; i >= 0; i--)
                {
                    if (Layers[i].Visible) Layers[i].draw(g, FrameView);
                }
            }

            // 【新增】绘制经纬网
            if (ShowGrid)
            {
                DrawGrid(g, screenRect.Width, screenRect.Height);
            }

            // 恢复状态
            g.Transform = oldTransform;
            g.Clip = oldClip;

            // 绘制边框和选中框
            g.DrawRectangle(Pens.Black, screenRect);
            DrawSelectionBox(g, screenRectF);
        }

        private void DrawGrid(Graphics g, float w, float h)
        {
            // 简单的经纬网示意绘制
            using (Pen p = new Pen(Color.Gray, 1) { DashStyle = DashStyle.Dot })
            using (Font f = new Font("Arial", 8))
            using (SolidBrush b = new SolidBrush(Color.Black))
            {
                int count = 4;
                // 竖线 (经度)
                for (int i = 1; i < count; i++)
                {
                    float x = w * i / count;
                    g.DrawLine(p, x, 0, x, h);
                    g.DrawString($"E{120 + i}°", f, b, x - 10, h - 15);
                }
                // 横线 (纬度)
                for (int i = 1; i < count; i++)
                {
                    float y = h * i / count;
                    g.DrawLine(p, 0, y, w, y);
                    g.DrawString($"N{30 + i}°", f, b, 2, y - 6);
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

        public XScaleBar(RectangleF bounds, ScaleBarStyle style)
        {
            this.Bounds = bounds;
            this.Style = style;
            this.Name = "比例尺";
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            RectangleF screenRect = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);

            // 绘制逻辑
            using (Pen p = new Pen(Color.Black, 2))
            using (SolidBrush bBlack = new SolidBrush(Color.Black))
            using (SolidBrush bWhite = new SolidBrush(Color.White))
            using (Font f = new Font("Arial", 10 * zoomScale))
            {
                float x = screenRect.X;
                float y = screenRect.Y;
                float w = screenRect.Width;
                float h = screenRect.Height;

                switch (Style)
                {
                    case ScaleBarStyle.Line:
                        float midY = y + h / 2;
                        g.DrawLine(p, x, midY, x + w, midY);
                        g.DrawLine(p, x, midY, x, midY - 5);
                        g.DrawLine(p, x + w / 2, midY, x + w / 2, midY - 5);
                        g.DrawLine(p, x + w, midY, x + w, midY - 5);
                        g.DrawString("0", f, bBlack, x - 5, midY + 2);
                        g.DrawString("500 km", f, bBlack, x + w - 30, midY + 2);
                        break;

                    case ScaleBarStyle.AlternatingBar:
                        float barH = h / 3;
                        RectangleF r1 = new RectangleF(x, y + barH, w / 4, barH);
                        RectangleF r2 = new RectangleF(x + w / 4, y + barH, w / 4, barH);
                        RectangleF r3 = new RectangleF(x + 2 * w / 4, y + barH, w / 4, barH);
                        RectangleF r4 = new RectangleF(x + 3 * w / 4, y + barH, w / 4, barH);
                        g.FillRectangle(bBlack, r1);
                        g.FillRectangle(bWhite, r2); g.DrawRectangle(p, r2.X, r2.Y, r2.Width, r2.Height);
                        g.FillRectangle(bBlack, r3);
                        g.FillRectangle(bWhite, r4); g.DrawRectangle(p, r4.X, r4.Y, r4.Width, r4.Height);
                        g.DrawString("0", f, bBlack, x, y + barH * 2);
                        g.DrawString("100 km", f, bBlack, x + w - 40, y + barH * 2);
                        break;

                    case ScaleBarStyle.DoubleLine:
                        float dY = y + h / 2;
                        g.DrawRectangle(p, x, dY - 3, w, 6);
                        g.FillRectangle(bBlack, x, dY - 3, w / 2, 6);
                        g.DrawString("1:10,000", f, bBlack, x, dY - 20);
                        break;
                }
            }
            DrawSelectionBox(g, screenRect);
        }
    }

    // ==========================================
    // 5. 文本元素
    // ==========================================
    public class XTextElement : XLayoutElement
    {
        public string Text = "地图标题";
        public Font Font = new Font("微软雅黑", 24, FontStyle.Bold);
        public SolidBrush Brush = new SolidBrush(Color.Black);

        public XTextElement(string text, float x, float y)
        {
            this.Name = "Text";
            this.Text = text;
            this.Bounds = new RectangleF(x, y, 100, 20);
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            RectangleF screenRect = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);
            Font drawFont = new Font(Font.FontFamily, Font.Size * zoomScale, Font.Style);
            g.DrawString(Text, drawFont, Brush, screenRect.Location);
            SizeF size = g.MeasureString(Text, drawFont);
            DrawSelectionBox(g, new RectangleF(screenRect.X, screenRect.Y, size.Width, size.Height));
        }
    }

    // ==========================================
    // 6. 布局页面管理
    // ==========================================
    public class XLayoutPage
    {
        public float WidthMM = 297; // A4
        public float HeightMM = 210;
        public List<XLayoutElement> Elements = new List<XLayoutElement>();

        public void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            float pixelPerMM = screenDpi / 25.4f;
            float w = WidthMM * pixelPerMM * zoomScale;
            float h = HeightMM * pixelPerMM * zoomScale;

            // 画阴影和纸张
            g.FillRectangle(Brushes.Gray, offsetX + 5, offsetY + 5, w, h);
            g.FillRectangle(Brushes.White, offsetX, offsetY, w, h);
            g.DrawRectangle(Pens.Black, offsetX, offsetY, w, h);

            // 画元素
            foreach (var ele in Elements)
            {
                ele.Draw(g, screenDpi, zoomScale, offsetX, offsetY);
            }
        }
    }
}