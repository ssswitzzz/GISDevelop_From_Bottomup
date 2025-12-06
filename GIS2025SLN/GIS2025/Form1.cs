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
        List<XVectorLayer> layers = new List<XVectorLayer>();
        XView view = null;
        Bitmap backwindow;
        Bitmap iconEyeOpen, iconEyeClose;
        XTileLayer basemapLayer;
        Timer timerDownloadCheck = new Timer();
        Timer timerZoom = new Timer();

        Point MouseDownLocation, MouseMovingLocation;
        XExploreActions currentMouseAction = XExploreActions.noaction;
        XExploreActions baseTool = XExploreActions.pan;

        private TreeView treeViewLayout;
        private TreeNode dropTargetNode = null;
        Point TreeMouseDownLocation;

        public FormMap()
        {
            InitializeComponent();

            // 界面初始化
            mapBox.Dock = DockStyle.Fill;
            myLayoutControl.Dock = DockStyle.Fill;
            mapBox.Visible = true;
            myLayoutControl.Visible = false;

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            view = new XView(new XExtent(-180, 180, -85, 85), mapBox.ClientRectangle);

            string myKey = "5e531967100311fcb8098b759848b71c";
            string url = $"https://t0.tianditu.gov.cn/DataServer?T=vec_w&x={{x}}&y={{y}}&l={{z}}&tk={myKey}";
            basemapLayer = new XTileLayer(url);
            basemapLayer.Name = "天地图矢量";

            mapBox.MouseWheel += mapBox_MouseWheel;
            mapBox.MouseLeave += MapBox_MouseLeave;
            mapBox.MouseDown += mapBox_MouseDown;
            mapBox.MouseMove += mapBox_MouseMove;
            mapBox.MouseUp += mapBox_MouseUp;
            mapBox.Paint += mapBox_Paint;
            mapBox.SizeChanged += mapBox_SizeChanged;

            timerZoom.Interval = 10; timerZoom.Tick += TimerZoom_Tick;
            timerDownloadCheck.Interval = 500; timerDownloadCheck.Tick += (s, e) => { UpdateMap(); }; timerDownloadCheck.Start();

            TreeNode baseNode = new TreeNode(basemapLayer.Name);
            baseNode.Tag = basemapLayer; baseNode.Checked = true;
            treeView1.Nodes.Add(baseNode);

            iconEyeOpen = Properties.Resources.icon_eye_open;
            iconEyeClose = Properties.Resources.icon_eye_close;
            treeView1.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            treeView1.ItemHeight = 26;

            treeView1.ItemDrag += treeView1_ItemDrag;
            treeView1.DragEnter += treeView1_DragEnter;
            treeView1.DragOver += treeView1_DragOver;
            treeView1.DragDrop += treeView1_DragDrop;
            treeView1.DragLeave += treeView1_DragLeave;
            treeView1.DrawNode += treeView1_DrawNode;
            treeView1.MouseDown += treeView1_MouseDown;
            treeView1.MouseMove += treeView1_MouseMove;
            treeView1.AfterCheck += treeView1_AfterCheck;

            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;

            InitLayoutTreeView();
            myLayoutControl.ElementChanged += (s, e) => { PopulateLayoutTree(); };

            view.Update(new XExtent(120, 125, 25, 35), mapBox.ClientRectangle);
        }

        private void InitLayoutTreeView()
        {
            treeViewLayout = new TreeView();
            treeViewLayout.Dock = DockStyle.Fill;
            treeViewLayout.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            treeViewLayout.ItemHeight = 26;
            treeViewLayout.Visible = false;
            treeViewLayout.CheckBoxes = false;

            if (treeView1.Parent != null) { treeView1.Parent.Controls.Add(treeViewLayout); treeViewLayout.BringToFront(); }

            treeViewLayout.DrawNode += treeView1_DrawNode;
            treeViewLayout.MouseDown += TreeViewLayout_MouseDown;
            treeViewLayout.ItemDrag += (s, e) => { DoDragDrop(e.Item, DragDropEffects.Move); };
            treeViewLayout.DragEnter += (s, e) => { e.Effect = DragDropEffects.Move; };
            treeViewLayout.DragOver += TreeViewLayout_DragOver;
            treeViewLayout.DragDrop += TreeViewLayout_DragDrop;
        }

        private void PopulateLayoutTree()
        {
            treeViewLayout.Nodes.Clear();
            if (myLayoutControl.Page == null) return;
            var elements = myLayoutControl.Page.Elements;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                var ele = elements[i];
                TreeNode node = new TreeNode(ele.Name);
                node.Tag = ele;
                node.Checked = ele.Visible;
                if (ele.IsSelected) treeViewLayout.SelectedNode = node;
                treeViewLayout.Nodes.Add(node);
            }
        }

        private void TreeViewLayout_MouseDown(object sender, MouseEventArgs e)
        {
            TreeViewHitTestInfo info = treeViewLayout.HitTest(e.Location);
            if (info.Node == null) return;
            TreeNode node = info.Node;
            XLayoutElement ele = node.Tag as XLayoutElement;

            if (e.Button == MouseButtons.Left)
            {
                int diff = e.X - node.Bounds.X;
                if (diff >= 0 && diff <= 24 && ele != null)
                {
                    ele.Visible = !ele.Visible;
                    node.Checked = ele.Visible;
                    myLayoutControl.Invalidate();
                    return;
                }
                treeViewLayout.SelectedNode = node;
                if (ele != null)
                {
                    foreach (var other in myLayoutControl.Page.Elements) other.IsSelected = false;
                    ele.IsSelected = true;
                    myLayoutControl.Invalidate();
                }
            }
        }

        private void TreeViewLayout_DragOver(object sender, DragEventArgs e)
        {
            Point pt = treeViewLayout.PointToClient(new Point(e.X, e.Y));
            TreeNode target = treeViewLayout.GetNodeAt(pt);
            if (target != null) { treeViewLayout.SelectedNode = target; e.Effect = DragDropEffects.Move; }
            else { e.Effect = DragDropEffects.None; }
        }

        private void TreeViewLayout_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode srcNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
            Point pt = treeViewLayout.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeViewLayout.GetNodeAt(pt);

            if (srcNode != null && targetNode != null && srcNode != targetNode)
            {
                treeViewLayout.Nodes.Remove(srcNode);
                treeViewLayout.Nodes.Insert(targetNode.Index, srcNode);
                treeViewLayout.SelectedNode = srcNode;

                List<XLayoutElement> newOrder = new List<XLayoutElement>();
                for (int i = treeViewLayout.Nodes.Count - 1; i >= 0; i--)
                {
                    newOrder.Add(treeViewLayout.Nodes[i].Tag as XLayoutElement);
                }
                myLayoutControl.Page.Elements = newOrder;
                myLayoutControl.Invalidate();
            }
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Text == "Layout")
            {
                mapBox.Visible = false; myLayoutControl.Visible = true;
                treeView1.Visible = false; treeViewLayout.Visible = true;

                List<XVectorLayer> visibleLayers = new List<XVectorLayer>();
                foreach (TreeNode node in treeView1.Nodes) { if (node.Checked && node.Tag is XVectorLayer layer) visibleLayers.Add(layer); }
                XExtent currentExtent = view.CurrentMapExtent;
                if (currentExtent == null && layers.Count > 0) currentExtent = layers[0].Extent;

                if (myLayoutControl != null && currentExtent != null) myLayoutControl.UpdateLayout(visibleLayers, this.basemapLayer, currentExtent);
                PopulateLayoutTree();
            }
            else
            {
                myLayoutControl.Visible = false; mapBox.Visible = true;
                treeViewLayout.Visible = false; treeView1.Visible = true;
                if (mapBox.Width > 0 && mapBox.Height > 0) { view.UpdateMapWindow(mapBox.ClientRectangle); UpdateMap(); }
            }
        }

        private void UpdateSelectionStatus()
        {
            int totalCount = 0;
            foreach (TreeNode node in treeView1.Nodes) { if (node.Tag is XVectorLayer layer) totalCount += layer.SelectedFeatures.Count; }
            lblSelectCount.Text = $"选中要素: {totalCount}";
        }

        private void MapBox_MouseLeave(object sender, EventArgs e) { lblCoordinates.Text = "Ready"; }

        private void UpdateMap()
        {
            if (view == null || mapBox.Width == 0 || mapBox.Height == 0) return;
            view.UpdateMapWindow(mapBox.ClientRectangle);
            if (backwindow != null) backwindow.Dispose();
            backwindow = new Bitmap(mapBox.Width, mapBox.Height);
            Graphics g = Graphics.FromImage(backwindow);
            g.Clear(Color.White);
            for (int i = treeView1.Nodes.Count - 1; i >= 0; i--)
            {
                TreeNode node = treeView1.Nodes[i];
                if (!node.Checked) continue;
                if (node.Tag is XVectorLayer vectorLayer) vectorLayer.draw(g, view);
                else if (node.Tag is XTileLayer tileLayer) tileLayer.Draw(g, view);
            }
            g.Dispose();
            mapBox.Invalidate();
        }

        private void mapBox_Paint(object sender, PaintEventArgs e)
        {
            if (backwindow == null) return;
            if (currentMouseAction == XExploreActions.pan) e.Graphics.DrawImage(backwindow, MouseMovingLocation.X - MouseDownLocation.X, MouseMovingLocation.Y - MouseDownLocation.Y);
            else
            {
                e.Graphics.DrawImage(backwindow, 0, 0);
                if ((currentMouseAction == XExploreActions.zoominbybox || currentMouseAction == XExploreActions.select) && Math.Abs(MouseDownLocation.X - MouseMovingLocation.X) > 0)
                {
                    int x = Math.Min(MouseDownLocation.X, MouseMovingLocation.X);
                    int y = Math.Min(MouseDownLocation.Y, MouseMovingLocation.Y);
                    int w = Math.Abs(MouseDownLocation.X - MouseMovingLocation.X);
                    int h = Math.Abs(MouseDownLocation.Y - MouseMovingLocation.Y);
                    Color boxColor = (currentMouseAction == XExploreActions.select) ? Color.Blue : Color.Red;
                    using (Pen pen = new Pen(boxColor, 2)) e.Graphics.DrawRectangle(pen, x, y, w, h);
                }
            }
        }

        private void mapBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) return;
            MouseDownLocation = e.Location;
            if (Control.ModifierKeys == Keys.Shift) { currentMouseAction = XExploreActions.zoominbybox; return; }
            if (e.Button == MouseButtons.Middle) { currentMouseAction = XExploreActions.pan; mapBox.Cursor = Cursors.Hand; return; }
            if (e.Button == MouseButtons.Left)
            {
                if (baseTool == XExploreActions.select) currentMouseAction = XExploreActions.select;
                else if (baseTool == XExploreActions.pan) currentMouseAction = XExploreActions.pan;
            }
        }

        private void mapBox_MouseMove(object sender, MouseEventArgs e)
        {
            XVertex v = view.ToMapVertex(e.Location);
            lblCoordinates.Text = $"X: {v.x:F2}, Y: {v.y:F2}";
            if (currentMouseAction == XExploreActions.noaction) return;
            MouseMovingLocation = e.Location;
            if (currentMouseAction == XExploreActions.pan || currentMouseAction == XExploreActions.zoominbybox || currentMouseAction == XExploreActions.select) mapBox.Invalidate();
        }

        private void mapBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (MouseDownLocation == e.Location && currentMouseAction != XExploreActions.select) { currentMouseAction = XExploreActions.noaction; return; }
            XVertex v1 = view.ToMapVertex(MouseDownLocation);
            XVertex v2 = view.ToMapVertex(e.Location);

            if (currentMouseAction == XExploreActions.zoominbybox)
            {
                if (Math.Abs(MouseDownLocation.X - e.X) > 2) view.Update(new XExtent(v1, v2), mapBox.ClientRectangle);
            }
            else if (currentMouseAction == XExploreActions.pan) view.OffsetCenter(v1, v2);
            else if (currentMouseAction == XExploreActions.select)
            {
                int dx = Math.Abs(MouseDownLocation.X - e.X);
                int dy = Math.Abs(MouseDownLocation.Y - e.Y);
                bool modify = (Control.ModifierKeys == Keys.Control);
                if (dx < 5 && dy < 5)
                {
                    double tol = view.ToMapDistance(5);
                    for (int i = 0; i < treeView1.Nodes.Count; i++)
                    {
                        TreeNode node = treeView1.Nodes[i];
                        if (node.Checked && node.Tag is XVectorLayer layer) layer.SelectByVertex(v1, tol, modify);
                    }
                }
                else
                {
                    XExtent extent = new XExtent(v1, v2);
                    foreach (TreeNode node in treeView1.Nodes)
                    {
                        if (node.Checked && node.Tag is XVectorLayer layer) layer.SelectByExtent(extent, modify);
                    }
                }
                UpdateSelectionStatus();
            }
            currentMouseAction = XExploreActions.noaction;
            mapBox.Cursor = (baseTool == XExploreActions.pan) ? Cursors.Hand : Cursors.Default;
            UpdateMap();
        }

        private void mapBox_MouseWheel(object sender, MouseEventArgs e)
        {
            view.SetZoomTarget(e.Location, e.Delta > 0);
            if (!timerZoom.Enabled) timerZoom.Start();
            UpdateMap();
        }

        private void mapBox_SizeChanged(object sender, EventArgs e) { UpdateMap(); }

        private void TimerZoom_Tick(object sender, EventArgs e)
        {
            if (view.UpdateBuffer()) { timerZoom.Stop(); view.TargetExtent = null; }
            UpdateMap();
        }

        private void btnOpenShapefile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog(); dlg.Filter = "Shapefile|*.shp";
            if (dlg.ShowDialog() != DialogResult.OK) return;
            XVectorLayer layer = XShapefile.ReadShapefile(dlg.FileName);
            layer.LabelOrNot = false;
            if (layers.Count == 0) layers.Add(layer); else layers[0] = layer;
            view.Update(layer.Extent, mapBox.ClientRectangle);
            UpdateMap();
        }

        // 【关键修复】名称改为 button_FullExtent_Click 以匹配设计器
        private void button_FullExtent_Click(object sender, EventArgs e)
        {
            if (treeView1.Nodes.Count == 0) return;
            XExtent full = null;
            foreach (TreeNode n in treeView1.Nodes) if (n.Tag is XVectorLayer l && l.Extent != null)
                {
                    if (full == null) full = new XExtent(l.Extent); else full.Merge(l.Extent);
                }
            if (full != null) { view.Update(full, mapBox.ClientRectangle); UpdateMap(); }
        }

        private void explore_button_Click(object sender, EventArgs e) { baseTool = XExploreActions.pan; currentMouseAction = XExploreActions.noaction; mapBox.Cursor = Cursors.Hand; }

        private void btnSelect_Click(object sender, EventArgs e) { baseTool = XExploreActions.select; currentMouseAction = XExploreActions.noaction; mapBox.Cursor = Cursors.Default; }

        private void button_ReadShp_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog(); dialog.Filter = "Shapefile|*.shp"; if (dialog.ShowDialog() != DialogResult.OK) return;
            foreach (string f in dialog.FileNames)
            {
                XVectorLayer l = XShapefile.ReadShapefile(f);
                l.Name = System.IO.Path.GetFileNameWithoutExtension(f); l.LabelOrNot = false;
                TreeNode n = new TreeNode(l.Name) { Tag = l, Checked = true };
                treeView1.Nodes.Insert(0, n); layers.Add(l);
            }
            if (layers.Count == 1) button_FullExtent_Click(null, null); // Call correct name
            UpdateMap();
        }

        private void btnAddMapFrame_Click(object sender, EventArgs e) { myLayoutControl.StartCreateMapFrame(); }
        private void btnAddNorthArrow_Click(object sender, EventArgs e) { cmsNorthArrow.Show(btnAddNorthArrow, 0, btnAddNorthArrow.Height); }
        private void toolStripMenuItemSimple_Click(object sender, EventArgs e) { SwitchToLayout(); myLayoutControl.StartCreateNorthArrow(NorthArrowStyle.Simple); }
        private void toolStripMenuItemCircle_Click(object sender, EventArgs e) { SwitchToLayout(); myLayoutControl.StartCreateNorthArrow(NorthArrowStyle.Circle); }
        private void toolStripMenuItemStar_Click(object sender, EventArgs e) { SwitchToLayout(); myLayoutControl.StartCreateNorthArrow(NorthArrowStyle.Star); }
        private void btnAddScaleBar_Click(object sender, EventArgs e) { cmsScaleBar.Show(btnAddScaleBar, 0, btnAddScaleBar.Height); }
        private void tsmiScaleLine_Click(object sender, EventArgs e) { SwitchToLayout(); myLayoutControl.StartCreateScaleBar(ScaleBarStyle.Line); }
        private void tsmiScaleBar_Click(object sender, EventArgs e) { SwitchToLayout(); myLayoutControl.StartCreateScaleBar(ScaleBarStyle.AlternatingBar); }
        private void tsmiScaleDouble_Click(object sender, EventArgs e) { SwitchToLayout(); myLayoutControl.StartCreateScaleBar(ScaleBarStyle.DoubleLine); }
        private void btnAddGrid_Click(object sender, EventArgs e) { SwitchToLayout(); myLayoutControl.StartToggleGrid(); MessageBox.Show("请点击地图框以 显示/隐藏 经纬网"); }
        private void SwitchToLayout() { if (tabControl1.SelectedTab != tabPage2) tabControl1.SelectedTab = tabPage2; }

        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e) { currentMouseAction = XExploreActions.noaction; DoDragDrop(e.Item, DragDropEffects.Move); }
        private void treeView1_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Move; }
        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e) { UpdateMap(); }

        private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Color backColor = ((e.State & TreeNodeStates.Selected) != 0) ? Color.FromArgb(204, 232, 255) : Color.White;
            using (SolidBrush brush = new SolidBrush(backColor)) e.Graphics.FillRectangle(brush, e.Bounds);

            Rectangle imgRect = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 4, 20, 20);
            Image imgToDraw = e.Node.Checked ? iconEyeOpen : iconEyeClose;
            if (imgToDraw != null) e.Graphics.DrawImage(imgToDraw, imgRect);

            Rectangle textRect = new Rectangle(e.Bounds.X + 30, e.Bounds.Y, e.Bounds.Width - 30, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Node.Text, ((TreeView)sender).Font, textRect, Color.Black, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            if (dropTargetNode != null && e.Node == dropTargetNode && sender == treeView1)
            {
                using (Pen linePen = new Pen(Color.FromArgb(0, 122, 204), 3))
                    e.Graphics.DrawLine(linePen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right, e.Bounds.Top);
            }
        }

        private void treeView1_DragOver(object sender, DragEventArgs e)
        {
            Point pt = treeView1.PointToClient(new Point(e.X, e.Y));
            TreeNode target = treeView1.GetNodeAt(pt);
            if (dropTargetNode != target) { dropTargetNode = target; treeView1.Invalidate(); }
            e.Effect = DragDropEffects.Move;
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode srcNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
            Point pt = treeView1.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeView1.GetNodeAt(pt);
            dropTargetNode = null; treeView1.Invalidate();
            if (srcNode == null) return;
            if (targetNode == null) { treeView1.Nodes.Remove(srcNode); treeView1.Nodes.Add(srcNode); }
            else if (targetNode != srcNode) { treeView1.Nodes.Remove(srcNode); treeView1.Nodes.Insert(targetNode.Index, srcNode); }
            treeView1.SelectedNode = srcNode;
            UpdateMap();
        }

        private void treeView1_DragLeave(object sender, EventArgs e) { dropTargetNode = null; treeView1.Invalidate(); }

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            TreeViewHitTestInfo info = treeView1.HitTest(e.Location);
            if (info.Node == null) return;
            TreeNode node = info.Node;
            if (e.Button == MouseButtons.Left)
            {
                TreeMouseDownLocation = e.Location;
                int diff = e.X - node.Bounds.X;
                if (diff >= 0 && diff <= 24) { node.Checked = !node.Checked; treeView1.Invalidate(); UpdateMap(); return; }
                treeView1.SelectedNode = node;
            }
            else if (e.Button == MouseButtons.Right)
            {
                treeView1.SelectedNode = node;
                if (node.Tag is XVectorLayer) contextMenuLayer.Show(treeView1, e.Location);
            }
        }

        private void treeView1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || treeView1.SelectedNode == null) return;
            Size dragSize = SystemInformation.DragSize;
            Rectangle dragRect = new Rectangle(new Point(TreeMouseDownLocation.X - dragSize.Width / 2, TreeMouseDownLocation.Y - dragSize.Height / 2), dragSize);
            if (!dragRect.Contains(e.Location)) DoDragDrop(treeView1.SelectedNode, DragDropEffects.Move);
        }

        private void 注记ToolStripMenuItem_Click(object sender, EventArgs e) { if (treeView1.SelectedNode?.Tag is XVectorLayer l) { l.LabelOrNot = !l.LabelOrNot; UpdateMap(); } }
        private void 注记属性ToolStripMenuItem_Click(object sender, EventArgs e) { if (treeView1.SelectedNode?.Tag is XVectorLayer l) { if (new FormLabelProperty(l).ShowDialog() == DialogResult.OK) UpdateMap(); } }
        private void 移除图层ToolStripMenuItem_Click(object sender, EventArgs e) { if (treeView1.SelectedNode != null && MessageBox.Show("移除?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { treeView1.Nodes.Remove(treeView1.SelectedNode); UpdateMap(); UpdateSelectionStatus(); } }
        private void 缩放至图层ToolStripMenuItem_Click(object sender, EventArgs e) { if (treeView1.SelectedNode?.Tag is XVectorLayer l && l.Extent != null) { view.Update(l.Extent, mapBox.ClientRectangle); UpdateMap(); } }
        private void 打开属性表ToolStripMenuItem_Click(object sender, EventArgs e) { if (treeView1.SelectedNode?.Tag is XVectorLayer l) new FormAttribute(l).Show(); }
    }
}