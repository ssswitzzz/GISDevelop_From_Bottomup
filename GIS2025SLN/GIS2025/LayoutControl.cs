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

            // 2. 创建标题
            XTextElement title = new XTextElement("My GIS Project", 100, 10);
            page.Elements.Add(title);

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
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Left)
            {
                isPanning = true;
                lastMouseLoc = e.Location;
            }
        }

        private void LayoutBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                offsetX += e.X - lastMouseLoc.X;
                offsetY += e.Y - lastMouseLoc.Y;
                lastMouseLoc = e.Location;
                layoutBox.Invalidate();
            }
        }

        private void LayoutBox_MouseUp(object sender, MouseEventArgs e)
        {
            isPanning = false;
        }
    }
}