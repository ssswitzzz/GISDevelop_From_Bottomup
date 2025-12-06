using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using XGIS; // 引用你的核心命名空间

namespace GIS2025
{
    public partial class LayoutControl : UserControl
    {
        // 核心数据
        private XLayoutPage page;

        // 视图交互参数
        private float zoomScale = 1.0f;
        private float offsetX = 50;
        private float offsetY = 50;
        private bool isPanning = false;
        private Point lastMouseLoc;
        private XLayoutElement selectedElement = null; // 当前选中的元素
        private bool isDraggingElement = false;        // 是否正在拖拽元素
        private PointF lastMouseMapLoc;                // 上一次鼠标在"纸张坐标系"中的位置

        public LayoutControl()
        {
            InitializeComponent();

            // 开启双缓冲，防止闪烁
            this.DoubleBuffered = true;

            // 绑定 PictureBox 事件
            layoutBox.MouseWheel += LayoutBox_MouseWheel;
            layoutBox.MouseDown += LayoutBox_MouseDown;
            layoutBox.MouseMove += LayoutBox_MouseMove;
            layoutBox.MouseUp += LayoutBox_MouseUp;
            layoutBox.Paint += LayoutBox_Paint;
        }

        // ==========================================
        // 核心方法：刷新布局数据
        // 当你切到这个 Tab 时，调用这个方法把地图数据传进来
        // ==========================================
        public void UpdateLayout(List<XVectorLayer> layers, XTileLayer baseLayer, XExtent currentExtent)
        {
            // 初始化一张 A4 纸
            page = new XLayoutPage();

            // 1. 创建地图框 (XMapFrame)
            // 把它放在纸张中间，留一点边距
            XMapFrame mapFrame = new XMapFrame(
                new RectangleF(20, 20, 257, 170), // A4(297x210) 减去边距
                layers,
                baseLayer,
                currentExtent
            );
            page.Elements.Add(mapFrame);

            // 强制重绘
            layoutBox.Invalidate();
        }

        // ==========================================
        // 绘图逻辑
        // ==========================================
        private void LayoutBox_Paint(object sender, PaintEventArgs e)
        {
            if (page == null) return; // 如果还没数据就不画

            // 获取屏幕 DPI，确保纸张物理尺寸在不同屏幕上看起来一致
            float dpi = e.Graphics.DpiX;
            page.Draw(e.Graphics, dpi, zoomScale, offsetX, offsetY);
            // 2. 画标尺 (覆盖在最上层)
            // 这里的 offsetX 和 offsetY 就是纸张左上角的屏幕坐标，标尺的0点将对齐这里
            DrawRulers(e.Graphics, dpi, zoomScale, offsetX, offsetY);
        }

        // ==========================================
        // 交互逻辑 (平移和缩放纸张)
        // ==========================================
        private void LayoutBox_MouseWheel(object sender, MouseEventArgs e)
        {
            // 滚轮缩放纸张视图
            if (e.Delta > 0) zoomScale *= 1.1f;
            else zoomScale /= 1.1f;
            layoutBox.Invalidate();
        }

        private void LayoutBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 【修正】e.Graphics 是不存在的，我们需要从控件创建 Graphics
                float dpi = 96;
                using (Graphics g = layoutBox.CreateGraphics())
                {
                    dpi = g.DpiX;
                }
                float pixelPerMM = dpi / 25.4f;

                // 倒序遍历（优先选中最上层的）
                bool hitSomething = false;
                for (int i = page.Elements.Count - 1; i >= 0; i--)
                {
                    var ele = page.Elements[i];

                    // 计算该元素当前的屏幕矩形
                    float ex = ele.Bounds.X * pixelPerMM * zoomScale + offsetX;
                    float ey = ele.Bounds.Y * pixelPerMM * zoomScale + offsetY;
                    float ew = ele.Bounds.Width * pixelPerMM * zoomScale;
                    float eh = ele.Bounds.Height * pixelPerMM * zoomScale;
                    RectangleF screenRect = new RectangleF(ex, ey, ew, eh);

                    if (screenRect.Contains(e.Location))
                    {
                        // 选中了它！
                        selectedElement = ele;
                        selectedElement.IsSelected = true;
                        isDraggingElement = true;
                        hitSomething = true;

                        // 把其他元素的选中状态取消
                        foreach (var other in page.Elements) if (other != ele) other.IsSelected = false;

                        break; // 找到了就停止
                    }
                }

                // 如果没点中任何元素，那就是点中了纸张，准备平移纸张
                if (!hitSomething)
                {
                    selectedElement = null;
                    foreach (var other in page.Elements) other.IsSelected = false; // 取消所有选中
                    isPanning = true;
                }

                lastMouseLoc = e.Location;
                layoutBox.Invalidate(); // 重绘以显示选中框
            }
            else if (e.Button == MouseButtons.Middle)
            {
                isPanning = true;
                lastMouseLoc = e.Location;
            }
        }

        // ==========================================
        // 修改 MouseMove：根据状态决定是移元素，还是移纸张
        // ==========================================
        private void LayoutBox_MouseMove(object sender, MouseEventArgs e)
        {
            float dx = e.X - lastMouseLoc.X;
            float dy = e.Y - lastMouseLoc.Y;

            // 情况A: 正在拖拽元素
            if (isDraggingElement && selectedElement != null)
            {
                using (Graphics g = layoutBox.CreateGraphics())
                {
                    // 把屏幕位移(dx) 换算回 纸张位移(mm)
                    float pixelPerMM = this.CreateGraphics().DpiX / 25.4f;
                    float mmDX = dx / zoomScale / pixelPerMM;
                    float mmDY = dy / zoomScale / pixelPerMM;

                    // 修改元素的位置
                    selectedElement.Bounds.X += mmDX;
                    selectedElement.Bounds.Y += mmDY;

                    layoutBox.Invalidate();

                }

            }
            // 情况B: 正在平移视图（纸张）
            else if (isPanning)
            {
                offsetX += dx;
                offsetY += dy;
                layoutBox.Invalidate();
            }

            lastMouseLoc = e.Location;
        }

        // ==========================================
        // 修改 MouseUp
        // ==========================================
        private void LayoutBox_MouseUp(object sender, MouseEventArgs e)
        {
            isPanning = false;
            isDraggingElement = false;
        }
        // ==========================================
        // LayoutControl.cs 新增的标尺代码
        // ==========================================

        // 定义标尺的宽度（像素）
        private const int RulerThickness = 25;

        // 绘制标尺的核心方法
        private void DrawRulers(Graphics g, float dpi, float zoom, float offX, float offY)
        {
            float pixelPerMM = dpi / 25.4f;

            // 1. 准备画笔和字体
            Pen linePen = Pens.Black;
            Font rulerFont = new Font("Arial", 7);
            Brush textBrush = Brushes.Black;

            // 2. 绘制标尺背景条 (浅灰色背景)
            // 顶部标尺背景
            g.FillRectangle(Brushes.WhiteSmoke, RulerThickness, 0, layoutBox.Width, RulerThickness);
            // 左侧标尺背景
            g.FillRectangle(Brushes.WhiteSmoke, 0, RulerThickness, RulerThickness, layoutBox.Height);
            // 左上角交汇处的方块
            g.FillRectangle(Brushes.LightGray, 0, 0, RulerThickness, RulerThickness);

            // 绘制边框线
            g.DrawLine(Pens.Gray, 0, RulerThickness, layoutBox.Width, RulerThickness); // 顶尺下缘
            g.DrawLine(Pens.Gray, RulerThickness, 0, RulerThickness, layoutBox.Height); // 左尺右缘

            // ===========================================
            // 3. 绘制顶部标尺 (X轴)
            // ===========================================
            // 我们假设纸张最大宽度大一点，比如 500mm，循环绘制刻度
            for (int i = 0; i <= 500; i++)
            {
                // 计算当前毫米刻度(i)在屏幕上的像素位置 X
                float screenX = offX + (i * pixelPerMM * zoom);

                // 优化：只画在屏幕可视范围内的刻度，且不覆盖左侧标尺
                if (screenX > RulerThickness && screenX < layoutBox.Width)
                {
                    // 判断刻度类型
                    if (i % 10 == 0) // 每10mm一个大刻度 + 数字
                    {
                        g.DrawLine(linePen, screenX, 0, screenX, 15); // 长线
                                                                      // 画数字 (居中)
                        string text = i.ToString();
                        SizeF size = g.MeasureString(text, rulerFont);
                        g.DrawString(text, rulerFont, textBrush, screenX - size.Width / 2, 12); // 文字略微靠下
                    }
                    else if (i % 5 == 0) // 每5mm一个中刻度
                    {
                        // 只有放大到一定程度才画中刻度，太小了画出来会糊成一团
                        if (zoom > 0.5)
                            g.DrawLine(linePen, screenX, 10, screenX, 15);
                    }
                    else // 每1mm一个小刻度
                    {
                        // 只有放大很大时才画毫米刻度
                        if (zoom > 1.0)
                            g.DrawLine(linePen, screenX, 12, screenX, 15);
                    }
                }
            }

            // ===========================================
            // 4. 绘制左侧标尺 (Y轴)
            // ===========================================
            for (int i = 0; i <= 500; i++)
            {
                // 计算当前毫米刻度(i)在屏幕上的像素位置 Y
                float screenY = offY + (i * pixelPerMM * zoom);

                if (screenY > RulerThickness && screenY < layoutBox.Height)
                {
                    if (i % 10 == 0) // 10mm
                    {
                        g.DrawLine(linePen, 0, screenY, 15, screenY);
                        // 画数字 (旋转90度比较复杂，这里先横着画，简单点)
                        string text = i.ToString();
                        SizeF size = g.MeasureString(text, rulerFont);
                        // 简单的横向文字，靠右对齐
                        g.DrawString(text, rulerFont, textBrush, 2, screenY - size.Height / 2);
                    }
                    else if (i % 5 == 0) // 5mm
                    {
                        if (zoom > 0.5)
                            g.DrawLine(linePen, 10, screenY, 15, screenY);
                    }
                    else // 1mm
                    {
                        if (zoom > 1.0)
                            g.DrawLine(linePen, 12, screenY, 15, screenY);
                    }
                }
            }

            // 绘制当前鼠标位置的动态指示线（红线），这个属于高级功能，可以先不做
        }
    }
}