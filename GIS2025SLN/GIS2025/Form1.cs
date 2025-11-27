using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XGIS;

namespace GIS2025
{
    public partial class FormMap : Form
    {
        // GIS核心变量
        List<XVectorLayer> layers = new List<XVectorLayer>();
        XView view = null;
        Bitmap backwindow;

        // 交互状态变量
        Point MouseDownLocation, MouseMovingLocation;
        XExploreActions currentMouseAction = XExploreActions.noaction;

        public FormMap()
        {
            InitializeComponent();
            mapBox.MouseWheel += mapBox_MouseWheel;
            // 手动订阅 TreeView 的事件
            // 【重要】现在的 View 大小要根据 mapBox 来定，而不是整个窗口
            // 请确保你设计视图里中间那个白色的控件名字叫 mapBox
            view = new XView(new XExtent(0, 1, 0, 1), mapBox.ClientRectangle);
            List<XVectorLayer> layers = new List<XVectorLayer>();
            // 初始化一个空的图层防止报错

        }
        private void UpdateMap()
        {
            if (view == null) return;
            // 如果 mapBox 还没准备好，就不画
            if (mapBox.Width == 0 || mapBox.Height == 0) return;

            // 1. 更新视图范围，告诉 View 现在画布有多大
            view.UpdateMapWindow(mapBox.ClientRectangle);

            // 2. 重新生成双缓冲图片 (backwindow)
            if (backwindow != null) backwindow.Dispose();
            backwindow = new Bitmap(mapBox.Width, mapBox.Height);

            // 3. 在内存里作画
            Graphics g = Graphics.FromImage(backwindow);
            g.Clear(Color.White); // 背景色设为白色
            foreach (XVectorLayer layer in layers)
            {
                // 只有当图层可见时才画 (预留功能，配合TreeView前面的勾选框)
                // if (layer.Visible) 
                layer.draw(g, view);
            }

            // 绘制图层
            // layer.LabelOrNot = checkBox1.Checked; // 如果你删了checkbox，这句要注释掉
            layers[0].draw(g, view);

            g.Dispose();

            // 4. 通知 mapBox 重绘自己 (触发 mapBox_Paint)
            mapBox.Invalidate();
        }

        // 这个事件要绑定到 mapBox 的 Paint 事件上！不是 Form 的 Paint！
        private void mapBox_Paint(object sender, PaintEventArgs e)
        {
            if (backwindow == null) return;

            // 处理动态交互的绘制（比如拖动时的残影，或者拉框的红框）
            if (currentMouseAction == XExploreActions.pan)
            {
                // 漫游时：画出偏移后的图片
                e.Graphics.DrawImage(backwindow,
                    MouseMovingLocation.X - MouseDownLocation.X,
                    MouseMovingLocation.Y - MouseDownLocation.Y);
            }
            else if (currentMouseAction == XExploreActions.zoominbybox)
            {
                // 拉框时：先画底图，再画上面的红框
                e.Graphics.DrawImage(backwindow, 0, 0);

                int x = Math.Min(MouseDownLocation.X, MouseMovingLocation.X);
                int y = Math.Min(MouseDownLocation.Y, MouseMovingLocation.Y);
                int width = Math.Abs(MouseDownLocation.X - MouseMovingLocation.X);
                int height = Math.Abs(MouseDownLocation.Y - MouseMovingLocation.Y);

                e.Graphics.DrawRectangle(
                    new Pen(new SolidBrush(Color.Red), 2),
                    x, y, width, height);
            }
            else
            {
                // 正常状态：直接把内存里的图贴出来
                e.Graphics.DrawImage(backwindow, 0, 0);
            }
        }

        // ====================================================================
        // 2. 鼠标交互逻辑 (全部绑定到 mapBox 上)
        // ====================================================================

        private void mapBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            MouseDownLocation = e.Location;

            // 按住 Shift 键进入拉框放大模式
            if (Control.ModifierKeys == Keys.Shift)
                currentMouseAction = XExploreActions.zoominbybox;
            // 默认就是漫游模式 (Pan)
            else
                currentMouseAction = XExploreActions.pan;
        }

        private void mapBox_MouseMove(object sender, MouseEventArgs e)
        {
            // 1. 显示坐标 (假设你底部的 Label 叫 lblCoordinates)
            XVertex mapVertex = view.ToMapVertex(e.Location);
            // 这里记得把 labelXY 换成你 StatusStrip 上那个 Label 的名字
            // labelXY.Text = $"X: {mapVertex.X:F2}, Y: {mapVertex.Y:F2}"; 

            // 2. 处理交互反馈
            MouseMovingLocation = e.Location;
            if (currentMouseAction == XExploreActions.zoominbybox ||
                currentMouseAction == XExploreActions.pan)
            {
                // 只有在拖动或拉框时才刷新，为了流畅
                mapBox.Invalidate();
            }
        }

        private void mapBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (MouseDownLocation == e.Location)
            {
                currentMouseAction = XExploreActions.noaction;
                return;
            }

            XVertex v1 = view.ToMapVertex(MouseDownLocation);
            XVertex v2 = view.ToMapVertex(e.Location);

            if (currentMouseAction == XExploreActions.zoominbybox)
            {
                XExtent extent = new XExtent(v1, v2);
                view.Update(extent, mapBox.ClientRectangle);
            }
            else if (currentMouseAction == XExploreActions.pan)
            {
                view.OffsetCenter(v1, v2);
            }

            currentMouseAction = XExploreActions.noaction;
            UpdateMap(); // 动作结束，生成新图
        }

        // 鼠标滚轮缩放
        private void mapBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
                view.ChangeView(XExploreActions.zoomin);
            else
                view.ChangeView(XExploreActions.zoomout);

            UpdateMap();
        }

        // 窗口大小改变时，地图也要重绘
        private void mapBox_SizeChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }

        // ====================================================================
        // 3. 顶部按钮功能区
        // ====================================================================

        // 按钮：读取 Shapefile
        private void btnOpenShapefile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Shapefile|*.shp";

            if (dialog.ShowDialog() != DialogResult.OK) return;

            layers[0] = XShapefile.ReadShapefile(dialog.FileName);
            layers[0].LabelOrNot = false;

            // 读完数据后，自动把视图缩放到全图范围
            view.Update(layers[0].Extent, mapBox.ClientRectangle);
            UpdateMap();
        }

        // 按钮：全图显示
        private void btnFullExtent_Click(object sender, EventArgs e)
        {
            if (layers[0] == null || layers[0].Extent == null) return;

            view.Update(new XExtent(layers[0].Extent), mapBox.ClientRectangle);
            UpdateMap();
        }

        private void explore_button_Click(object sender, EventArgs e)
        {
            // 切换当前动作为 Pan (平移)
            currentMouseAction = XExploreActions.pan;
            mapBox.Cursor = Cursors.Hand;
        }

        private void button_ReadShp_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Shapefile|*.shp"; 
            if (dialog.ShowDialog() != DialogResult.OK) return;
            XVectorLayer newlayer = XShapefile.ReadShapefile(dialog.FileName);
            newlayer.LabelOrNot = false; // 默认不显示标注，防止太乱
            newlayer.Name = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
            layers.Add(newlayer);
            treeView1.Nodes.Add(newlayer.Name);

            if (layers.Count > 0) {
                view.Update(newlayer.Extent, mapBox.ClientRectangle);
            }
            UpdateMap();
        }

        private void button_FullExtent_Click(object sender, EventArgs e)
        {
            if (layers[0] == null || layers[0].Extent == null) return;
            view.Update(new XExtent(layers[0].Extent), mapBox.ClientRectangle);
            UpdateMap();
        }



        // 按钮：漫游 (其实默认就是漫游，这个按钮可以用来重置状态)
        private void btnExplore_Click(object sender, EventArgs e)
        {
            currentMouseAction = XExploreActions.pan;
            // 可以给用户一个提示，或者把鼠标样式变一下
            mapBox.Cursor = Cursors.Hand;
        }
    }
}