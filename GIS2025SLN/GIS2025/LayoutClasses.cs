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
                // 注意这里传入 screenRect 是为了宽高，传入 FrameView 是为了坐标计算
                DrawGrid(g, screenRect, FrameView);
            }

            // 恢复状态
            g.Transform = oldTransform;
            g.Clip = oldClip;

            // 绘制边框和选中框
            g.DrawRectangle(Pens.Black, screenRect);
            DrawSelectionBox(g, screenRectF);
        }

        // 修改 DrawGrid 方法
        private void DrawGrid(Graphics g, Rectangle screenRect, XView view)
        {
            XExtent extent = view.CurrentMapExtent;
            if (extent == null) return;

            // 1. 计算合适的间隔 (经纬度)
            // 假设我们在屏幕上至少每隔 50-100 像素画一条线
            double mapWidth = extent.GetWidth();
            // 估算一下：屏幕宽 screenRect.Width (px) 对应 mapWidth (度)
            // 我们希望每隔约 100px 画一条，那大概是多少度？
            int targetTickCount = (int)(screenRect.Width / 100.0);
            if (targetTickCount < 2) targetTickCount = 2;

            double step = XGISMath.CalculateNiceInterval(mapWidth, targetTickCount);

            // 2. 计算起始和结束经纬度 (取整)
            double startX = Math.Ceiling(extent.GetMinX() / step) * step;
            double startY = Math.Ceiling(extent.GetMinY() / step) * step;

            using (Pen p = new Pen(Color.Gray, 1) { DashStyle = DashStyle.Dot })
            using (Font f = new Font("Arial", 8))
            using (SolidBrush b = new SolidBrush(Color.Black))
            {
                // 3. 画经线 (X)
                for (double x = startX; x <= extent.GetMaxX(); x += step)
                {
                    // 将地图坐标转为 Frame 内的屏幕坐标
                    Point pt = view.ToScreenPoint(new XVertex(x, extent.GetMinY()));
                    // view.ToScreenPoint 返回的是相对于 view 窗口的坐标 (0,0 在 Frame 左上角)
                    // 但是 view 的窗口大小可能和 screenRect 不完全一致（因为缩放），稳妥起见我们用比例算

                    // 更稳妥的方法：直接调用 ToScreenPoint，得到的是相对于 UpdateMapWindow 时传入的 rect 的坐标
                    // 因为我们在 Draw 里调用了 UpdateMapWindow(0,0, w, h)，所以这个 pt.X 就是相对于 MapFrame 左上角的

                    // 过滤掉超出范围的线 (Floating point error)
                    if (pt.X < 0 || pt.X > screenRect.Width) continue;

                    g.DrawLine(p, pt.X, 0, pt.X, screenRect.Height);

                    // 写文字 (保留2位小数或者更少)
                    string label = x.ToString("0.##") + "°";
                    g.DrawString(label, f, b, pt.X - 15, screenRect.Height - 15);
                }

                // 4. 画纬线 (Y)
                for (double y = startY; y <= extent.GetMaxY(); y += step)
                {
                    Point pt = view.ToScreenPoint(new XVertex(extent.GetMinX(), y));

                    if (pt.Y < 0 || pt.Y > screenRect.Height) continue;

                    g.DrawLine(p, 0, pt.Y, screenRect.Width, pt.Y);

                    string label = y.ToString("0.##") + "°";
                    g.DrawString(label, f, b, 2, pt.Y - 6);
                }
            }
        }

        // 记得修改 override Draw 方法调用 DrawGrid 的地方：
        // DrawGrid(g, screenRect, FrameView); // <--- 传入 View
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
        public XMapFrame LinkedMapFrame; // 【新增】绑定的地图框

        public XScaleBar(RectangleF bounds, ScaleBarStyle style, XMapFrame mapFrame)
        {
            this.Bounds = bounds;
            this.Style = style;
            this.Name = "比例尺";
            this.LinkedMapFrame = mapFrame;
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            RectangleF screenRect = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);

            // 1. 如果没有绑定地图，画一个假的占位符
            if (LinkedMapFrame == null) { /* 画个框或者旧逻辑 */ return; }

            // 2. 【核心】计算当前比例尺框框代表的实际距离
            // 在屏幕上，比例尺的宽度是 screenRect.Width (像素)
            // 我们需要知道这 screenRect.Width 对应地图上多少米

            // 获取 View
            XView view = LinkedMapFrame.FrameView;

            // 模拟两个点：比例尺左端和右端 (在地图坐标系中)
            // 注意：因为比例尺是画在纸上的，我们要反算回 Map 坐标有点麻烦
            // 更简单的方法：利用 View 的 Scale。
            // View.ToMapDistance(pixels) 返回的是地图单位（比如度）

            double mapUnitWidth = view.ToMapDistance((int)screenRect.Width);

            // 假设地图单位是经纬度，转成米
            // 取地图中心点作为参考纬度
            XVertex center = view.CurrentMapExtent.GetCenter();
            double metersTotal = XGISMath.GetDistInMeters(center, new XVertex(center.x + mapUnitWidth, center.y));

            // 3. 计算“好看的数字”
            // 比如 metersTotal = 4321 米 -> 我们希望显示 2000米 或 4000米，而不是 4321
            double niceDistance = GetNiceRoundNumber(metersTotal);

            // 4. 根据好看的数字，反推比例尺应该画多长
            // 比如 框框宽 100px 代表 4321米，那 4000米 应该画多少 px?
            float drawWidth = (float)(screenRect.Width * (niceDistance / metersTotal));

            // 5. 格式化文字 (m 或 km)
            string labelText;
            if (niceDistance >= 1000) labelText = (niceDistance / 1000).ToString("0") + " km";
            else labelText = niceDistance.ToString("0") + " m";

            // 6. 开始绘图 (使用 drawWidth 而不是 screenRect.Width)
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
                        g.DrawLine(p, x, midY, x + drawWidth, midY); // 横线只画到 niceDistance 的位置
                        g.DrawLine(p, x, midY, x, midY - 5); // 左竖
                        g.DrawLine(p, x + drawWidth, midY, x + drawWidth, midY - 5); // 右竖
                        // 中间分段
                        g.DrawLine(p, x + drawWidth / 2, midY, x + drawWidth / 2, midY - 5);

                        g.DrawString("0", f, bBlack, x - 5, midY + 2);
                        g.DrawString(labelText, f, bBlack, x + drawWidth - 20, midY + 2);
                        break;

                    case ScaleBarStyle.AlternatingBar:
                        float barH = h / 3;
                        // 分4段
                        float segW = drawWidth / 4;
                        g.FillRectangle(bBlack, x, y + barH, segW, barH);
                        g.FillRectangle(bWhite, x + segW, y + barH, segW, barH); g.DrawRectangle(p, x + segW, y + barH, segW, barH);
                        g.FillRectangle(bBlack, x + 2 * segW, y + barH, segW, barH);
                        g.FillRectangle(bWhite, x + 3 * segW, y + barH, segW, barH); g.DrawRectangle(p, x + 3 * segW, y + barH, segW, barH);

                        g.DrawString("0", f, bBlack, x, y + barH * 2);
                        g.DrawString(labelText, f, bBlack, x + drawWidth - 20, y + barH * 2);
                        break;

                    case ScaleBarStyle.DoubleLine:
                        // ... 类似逻辑，使用 drawWidth ...
                        break;
                }
            }
            DrawSelectionBox(g, screenRect);
        }

        // 辅助：找一个比 val 小的最大的整洁整数 (1, 2, 5 开头)
        private double GetNiceRoundNumber(double val)
        {
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(val)));
            double normalized = val / magnitude; // 比如 4.321

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
    // 在 XGIS 命名空间下添加
    public static class XGISMath
    {
        // 计算“好看的间隔” (例如 0.1, 0.2, 0.5, 1, 2, 5, 10...)
        public static double CalculateNiceInterval(double range, int targetTickCount)
        {
            // 1. 粗略间隔
            double roughStep = range / targetTickCount;

            // 2. 计算数量级 (比如 0.03 -> 0.01, 300 -> 100)
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
            double normalizedStep = roughStep / magnitude;

            // 3. 归一化到 1, 2, 5
            double niceStep;
            if (normalizedStep < 1.5) niceStep = 1;
            else if (normalizedStep < 3) niceStep = 2;
            else if (normalizedStep < 7) niceStep = 5;
            else niceStep = 10;

            return niceStep * magnitude;
        }

        // 简单估算：经纬度转米 (用于比例尺)
        // 假设纬度 lat 处的 1 度经度 = 111320 * cos(lat) 米
        // 1 度纬度 = 110574 米
        public static double GetDistInMeters(XVertex p1, XVertex p2)
        {
            // 这里简化处理，假设是平面坐标或墨卡托，或者简单的球面估算
            // 如果你的坐标系是经纬度 (Lat/Lon)
            double avgLat = (p1.y + p2.y) / 2.0;
            double radLat = avgLat * Math.PI / 180.0;

            double dx = Math.Abs(p1.x - p2.x); // 经度差
            double dy = Math.Abs(p1.y - p2.y); // 纬度差

            double lx = dx * 111320 * Math.Cos(radLat); // 经度对应的米
            double ly = dy * 110574; // 纬度对应的米

            return Math.Sqrt(lx * lx + ly * ly);
        }
    }