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
        //GIS数据库，虽然只有一个图层
        XVectorLayer layer;

        XView view = null;

        Bitmap backwindow;

        Point MouseDownLocation, MouseMovingLocation;
        XExploreActions currentMouseAction = XExploreActions.noaction;

        public FormMap()
        {
            InitializeComponent();
            DoubleBuffered = true;

            view = new XView(new XExtent(0,1,0,1), ClientRectangle);

            layer = new XVectorLayer("point_layer", SHAPETYPE.point);
        }

        private void FormMap_MouseClick(object sender, MouseEventArgs e)
        {
            //XVertex onevertex =view.ToMapVertex(e.Location);

            //double mindistance = Double.MaxValue;
            //int findid = -1;
            //for (int i = 0; i <layer.FeatureCount(); i++)
            //{
            //    double distance = layer.GetFeature(i).Distance(onevertex);
            //    if (distance < mindistance)
            //    {
            //        mindistance = distance;
            //        findid = i;
            //    }
            //}

            //XVertex anotherVertex = view.ToMapVertex(new Point(e.X+3, e.Y + 3));

            //double minClickDistance=onevertex.Distance(anotherVertex);

            //if (findid == -1)
            //{
            //    MessageBox.Show("先添加，再点击！");
            //}
            //else if (mindistance> minClickDistance)
            //{
            //    MessageBox.Show("太远了！");
            //}
            //else
            //{
            //    MessageBox.Show("找到了：" + layer.GetFeature(findid).getAttribute(0));
            //}
        }

        private void UpdateMap()
        {
            //如果地图窗口被最小化了，就不用绘制了
            if (ClientRectangle.Width==0|| ClientRectangle.Height == 0) return;
            //更新view，以确保其地图窗口尺寸是正确的
            view.UpdateMapWindow(ClientRectangle);
            //如果背景窗口不为空，则先清除，回收内存资源
            if (backwindow != null) backwindow.Dispose();
            //根据最新的地图窗口尺寸建立背景窗口
            backwindow = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
            //在背景窗口上绘图
            Graphics g = Graphics.FromImage(backwindow);
            //清空窗口
            g.FillRectangle(new SolidBrush(Color.Gray), ClientRectangle);
            //g.Clear(Color.White);
            //绘制空间对象
            layer.LabelOrNot = checkBox1.Checked;
            layer.draw(g, view);
            //回收绘图工具
            g.Dispose();
            //重绘前景窗口
            Invalidate();
        }

        private void bRefresh_Click(object sender, EventArgs e)
        {
            UpdateMap();
        }

        private void FormMap_SizeChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }

        private void FormMap_MouseMove(object sender, MouseEventArgs e)
        {
            XVertex mapVertex=view.ToMapVertex(e.Location);
            string xy=mapVertex.x.ToString("f2")+","+mapVertex.y.ToString("f2");
            labelXY.Text = xy;

            MouseMovingLocation = e.Location;
            if (currentMouseAction == XExploreActions.zoominbybox ||
                currentMouseAction == XExploreActions.pan||
                currentMouseAction == XExploreActions.select)
            {
                Invalidate();
            }
        }

        private void bFullExtent_Click(object sender, EventArgs e)
        {
            view.Update(
                new XExtent(layer.Extent),
                ClientRectangle);
            UpdateMap();
        }

        private void FormMap_Paint(object sender, PaintEventArgs e)
        {
            if (backwindow == null) return;
            if (currentMouseAction == XExploreActions.pan)
            {
                e.Graphics.DrawImage(backwindow,
                    MouseMovingLocation.X - MouseDownLocation.X,
                    MouseMovingLocation.Y - MouseDownLocation.Y);
            }
            else if (currentMouseAction == XExploreActions.zoominbybox||
                currentMouseAction == XExploreActions.select)
            {
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
                e.Graphics.DrawImage(backwindow, 0, 0);
            }

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }

        private void FormMap_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            MouseDownLocation = e.Location;
            if (Control.ModifierKeys == Keys.Shift)
                currentMouseAction = XExploreActions.zoominbybox;
            else if (Control.ModifierKeys == Keys.Alt ||
                Control.ModifierKeys == (Keys.Alt | Keys.Control))
                currentMouseAction = XExploreActions.select;
            else
                currentMouseAction = XExploreActions.pan;
        }

        private void FormMap_MouseUp(object sender, MouseEventArgs e)
        {
            XVertex v1 = view.ToMapVertex(MouseDownLocation);
            XVertex v2 = view.ToMapVertex(e.Location);

            if (MouseDownLocation == e.Location)
            {
                if (currentMouseAction == XExploreActions.select)  //现在是点选
                {
                    layer.SelectByVertex(v2,view.ToMapDistance(5),
                            Control.ModifierKeys == (Keys.Alt | Keys.Control));
                    UpdateMap();
                }

                currentMouseAction = XExploreActions.noaction;
                return;
            }


            if (currentMouseAction == XExploreActions.zoominbybox)
            {
                XExtent extent = new XExtent(v1, v2);
                view.Update(extent, ClientRectangle);
            }
            else if (currentMouseAction == XExploreActions.pan)
            {
                view.OffsetCenter(v1, v2);
            }
            else if (currentMouseAction == XExploreActions.select) //框选
            {
                XExtent extent = new XExtent(v1, v2);
                layer.SelectByExtent(extent,
                            Control.ModifierKeys == (Keys.Alt | Keys.Control));
            }
            currentMouseAction = XExploreActions.noaction;
            UpdateMap();
        }

        private void bReadShapefile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Shapfile|*.shp";






            if (dialog.ShowDialog() != DialogResult.OK) return;





            layer = XShapefile.ReadShapefile(dialog.FileName);
            layer.LabelOrNot = false;
            view.Update(layer.Extent, ClientRectangle);
            UpdateMap();

        }

        private void FormMap_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta>0)
            {
                view.ChangeView(XExploreActions.zoomin);
            }
            else
            {
                view.ChangeView(XExploreActions.zoomout);
            }
            UpdateMap();
        }

        private void bZoomIn_Click(object sender, EventArgs e)
        {
            view.ChangeView(XExploreActions.zoomin);
            UpdateMap();
        }

        private void bZoomOut_Click(object sender, EventArgs e)
        {
            view.ChangeView(XExploreActions.zoomout);
            UpdateMap();
        }

        private void bOpenAttribute_Click(object sender, EventArgs e)
        {
            FormAttribute form = new FormAttribute(layer);
            form.Show();
        }

        private void bReadMyFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "myfile|*.gis";

            if (dialog.ShowDialog() != DialogResult.OK) return;

            layer = XMyFile.ReadFile(dialog.FileName);

            view.Update(layer.Extent, ClientRectangle);
            UpdateMap();
        }

        private void bWriteMyFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "myfile|*.gis";

            if (dialog.ShowDialog() != DialogResult.OK) return;

            XMyFile.WriteFile(layer, dialog.FileName);
        }

        private void ExploreButton_Click(object sender, EventArgs e)
        {
            XExploreActions action= XExploreActions.zoomin;

            if (sender == bZoomIn) action = XExploreActions.zoomin;
            else if (sender == bZoomOut) action = XExploreActions.zoomout;
            else if (sender == bMoveDown) action = XExploreActions.movedown;
            else if (sender == bMoveLeft) action = XExploreActions.moveleft;
            else if (sender == bMoveRight) action = XExploreActions.moveright;
            else if (sender == bMoveUp) action = XExploreActions.moveup;

            view.ChangeView(action);
            UpdateMap();
        }
    }
}
