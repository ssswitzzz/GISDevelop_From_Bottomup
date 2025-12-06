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
    public class XMapFrame : XLayoutElement
    {
        public List<XVectorLayer> Layers; // 要画哪些图层
        public XTileLayer BaseLayer;      // 底图
        public XView FrameView;           // 地图框内部的视图控制（控制比例尺、中心点）

        public XMapFrame(RectangleF bounds, List<XVectorLayer> layers, XTileLayer baseLayer, XExtent currentExtent)
        {
            this.Bounds = bounds;
            this.Name = "Map Frame";
            this.Layers = layers;
            this.BaseLayer = baseLayer;

            // 这里的 FrameView 需要根据纸张上的大小来初始化
            // 我们暂时给它一个空的 Rectangle，等 Draw 的时候再更新实际像素大小
            this.FrameView = new XView(new XExtent(currentExtent), new Rectangle(0, 0, 1, 1));
        }

        public override void Draw(Graphics g, float screenDpi, float zoomScale, float offsetX, float offsetY)
        {
            // 1. 计算地图框在屏幕上的实际像素区域
            RectangleF screenRectF = MMToPixel(screenDpi, zoomScale, offsetX, offsetY);
            Rectangle screenRect = new Rectangle((int)screenRectF.X, (int)screenRectF.Y, (int)screenRectF.Width, (int)screenRectF.Height);

            // 2. 更新 FrameView 的窗口大小，确保比例尺计算正确
            FrameView.UpdateMapWindow(screenRect);

            // 3. 限制绘图区域 (Clip)，防止地图画出框外
            Region oldClip = g.Clip;
            g.SetClip(screenRect);

            // 4. 绘制背景（白色底）
            g.FillRectangle(Brushes.White, screenRect);

            // 5. 绘制底图 (如果有)
            if (BaseLayer != null && BaseLayer.Visible)
            {
                BaseLayer.Draw(g, FrameView);
            }

            // 6. 绘制矢量图层 (倒序，和 FormMap 一样)
            // 注意：这里我们直接复用 XVectorLayer.draw，因为它接受 XView
            // 只要传入的是 FrameView (对应纸张上的区域)，它就能画对！
            if (Layers != null)
            {
                for (int i = Layers.Count - 1; i >= 0; i--)
                {
                    if (Layers[i].Visible)
                        Layers[i].draw(g, FrameView);
                }
            }

            // 7. 绘制边框
            g.DrawRectangle(Pens.Black, screenRect);

            // 恢复 Clip
            g.Clip = oldClip;

            // 绘制选中状态
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