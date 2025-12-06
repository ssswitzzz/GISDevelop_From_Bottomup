using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using XGIS;

namespace XGIS
{
    // 1. 布局元素的基类
    // 定义所有在纸上东西的共性：有位置(Rect)，能被选中，能画出来
    public abstract class XLayoutElement
    {
        // 纸张上的位置（单位：毫米 mm），比如 (20, 20, 200, 150)
        public RectangleF Bounds;
        public bool IsSelected = false;

        // 名字，用于图层列表显示
        public string Name = "Element";

        // 核心绘制方法
        // g: 画笔
        // screenDpi: 屏幕DPI，用于将毫米换算成像素
        // zoomScale: 布局视图的缩放比例（比如用户把纸张放大了200%看细节）
        // offsetX/Y: 纸张在屏幕上的偏移量
        public abstract void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY);

        // 辅助函数：将毫米转换为屏幕像素
        protected RectangleF MMToPixel(float dpi, float zoom, float offX, float offY)
        {
            float pixelPerMM = dpi / 25.4f; // 1英寸=25.4mm
            float x = Bounds.X * pixelPerMM * zoom + offX;
            float y = Bounds.Y * pixelPerMM * zoom + offY;
            float w = Bounds.Width * pixelPerMM * zoom;
            float h = Bounds.Height * pixelPerMM * zoom;
            return new RectangleF(x, y, w, h);
        }

        // 绘制选中框（当被选中时显示一个小蓝框）
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

    // 2. 地图框元素 (Map Frame)
    // 这是最复杂的元素，它里面要画地图
    // LayoutClasses.cs

    public class XMapFrame : XLayoutElement
    {
        public List<XVectorLayer> Layers;
        public XTileLayer BaseLayer;
        public XView FrameView;

        public XMapFrame(RectangleF bounds, List<XVectorLayer> layers, XTileLayer baseLayer, XExtent currentExtent)
        {
            this.Bounds = bounds;
            this.Name = "Map Frame";
            this.Layers = layers;
            this.BaseLayer = baseLayer;
            // 初始化时给一个临时的大小
            this.FrameView = new XView(new XExtent(currentExtent), new Rectangle(0, 0, 1, 1));
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            // 1. 计算地图框在屏幕上的实际位置和大小
            RectangleF screenRectF = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);
            // 转为整数矩形
            Rectangle screenRect = new Rectangle((int)screenRectF.X, (int)screenRectF.Y, (int)screenRectF.Width, (int)screenRectF.Height);

            // 2. 【核心修复】更新视图窗口
            // 注意：这里我们告诉 FrameView，你的窗口是从 (0,0) 开始的，宽即宽，高即高。
            // 这样 FrameView 计算出来的坐标就是相对于框左上角的相对坐标。
            FrameView.UpdateMapWindow(new Rectangle(0, 0, screenRect.Width, screenRect.Height));

            // 3. 保存当前的绘图状态 (Clip 和 Transform)
            Region oldClip = g.Clip;
            System.Drawing.Drawing2D.Matrix oldTransform = g.Transform;

            // 4. 设置裁切区域 (防止画出框外)
            g.SetClip(screenRect);

            // 5. 【核心修复】设置坐标平移
            // 所有的绘图指令都会自动加上 (screenRect.X, screenRect.Y)
            g.TranslateTransform(screenRect.X, screenRect.Y);

            // 6. 绘制背景 (相对于框的 0,0)
            g.FillRectangle(Brushes.White, 0, 0, screenRect.Width, screenRect.Height);

            // 7. 绘制图层
            // 因为已经做了 TranslateTransform，这里画图时，Engine 算出的 (0,0) 会被画在屏幕的 (screenRect.X, screenRect.Y)
            if (BaseLayer != null && BaseLayer.Visible)
            {
                BaseLayer.Draw(g, FrameView);
            }

            if (Layers != null)
            {
                // 倒序绘制
                for (int i = Layers.Count - 1; i >= 0; i--)
                {
                    if (Layers[i].Visible)
                        Layers[i].draw(g, FrameView);
                }
            }

            // 8. 恢复绘图状态 (非常重要！否则后面的元素位置会乱)
            g.Transform = oldTransform;
            g.Clip = oldClip;

            // 9. 绘制边框和选中框 (这部分是在 Layout 坐标系下的，所以恢复 Transform 后再画)
            g.DrawRectangle(Pens.Black, screenRect);
            DrawSelectionBox(g, screenRectF);
        }
    }

    // 3. 文本元素 (Title)
    public class XTextElement : XLayoutElement
    {
        public string Text = "地图标题";
        public Font Font = new Font("微软雅黑", 24, FontStyle.Bold);
        public SolidBrush Brush = new SolidBrush(Color.Black);

        public XTextElement(string text, float x, float y)
        {
            this.Name = "Text";
            this.Text = text;
            this.Bounds = new RectangleF(x, y, 100, 20); // 初始大小，后面可以根据文字自动算
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            RectangleF screenRect = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);

            // 简单的文字绘制
            // 注意：字体大小通常是 Point，不需要手动乘缩放，Graphics 会处理，或者手动计算
            // 为了所见即所得，最好创建一个缩放后的 Font
            Font drawFont = new Font(Font.FontFamily, Font.Size * zoomScale, Font.Style);

            g.DrawString(Text, drawFont, Brush, screenRect.Location);

            // 更新一下 Bounds 的宽高，方便点击选中
            SizeF size = g.MeasureString(Text, drawFont);
            // 这里要反算回毫米，暂时先略过，只做简单的显示

            DrawSelectionBox(g, new RectangleF(screenRect.X, screenRect.Y, size.Width, size.Height));
        }
    }

    // 4. 布局页面管理类
    public class XLayoutPage
    {
        public float WidthMM = 297; // A4 横向
        public float HeightMM = 210;
        public List<XLayoutElement> Elements = new List<XLayoutElement>();

        public void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            // 1. 画纸张阴影 (好看一点)
            float pixelPerMM = screenDpi / 25.4f;
            float w = WidthMM * pixelPerMM * zoomScale;
            float h = HeightMM * pixelPerMM * zoomScale;
            g.FillRectangle(Brushes.Gray, offsetX + 5, offsetY + 5, w, h);

            // 2. 画纸张本身
            g.FillRectangle(Brushes.White, offsetX, offsetY, w, h);
            g.DrawRectangle(Pens.Black, offsetX, offsetY, w, h);

            // 3. 画所有元素
            foreach (var ele in Elements)
            {
                ele.Draw(g, screenDpi, zoomScale, offsetX, offsetY);
            }
        }
    }
}