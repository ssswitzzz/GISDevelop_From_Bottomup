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
        // 核心数据
        List<XVectorLayer> layers = new List<XVectorLayer>();
        XView view = null;
        Bitmap backwindow;

        // 资源与工具
        Bitmap iconEyeOpen, iconEyeClose;
        XTileLayer basemapLayer;
        Timer timerDownloadCheck = new Timer();
        Timer timerZoom = new Timer();

        // 鼠标交互状态
        Point MouseDownLocation, MouseMovingLocation;
        XExploreActions currentMouseAction = XExploreActions.noaction;
        XExploreActions baseTool = XExploreActions.pan;

        // Layout 相关变量
        private TreeView treeViewLayout;
        private TreeNode dropTargetNode = null;
        Point TreeMouseDownLocation;

        public FormMap()
        {
            InitializeComponent();

            // 1. 界面初始化
            mapBox.Dock = DockStyle.Fill;
            myLayoutControl.Dock = DockStyle.Fill;
            mapBox.Visible = true;
            myLayoutControl.Visible = false;

            // 2. 视图与网络设置
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            view = new XView(new XExtent(-180, 180, -85, 85), mapBox.ClientRectangle);

            // 3. 底图设置
            string myKey = "5e531967100311fcb8098b759848b71c";
            string url = $"https://t0.tianditu.gov.cn/DataServer?T=vec_w&x={{x}}&y={{y}}&l={{z}}&tk={myKey}";
            basemapLayer = new XTileLayer(url);
            basemapLayer.Name = "天地图矢量";

            // 4. MapBox 事件绑定
            mapBox.MouseWheel += mapBox_MouseWheel;
            mapBox.MouseLeave += MapBox_MouseLeave;
            mapBox.MouseDown += mapBox_MouseDown;
            mapBox.MouseMove += mapBox_MouseMove;
            mapBox.MouseUp += mapBox_MouseUp;
            mapBox.Paint += mapBox_Paint;
            mapBox.SizeChanged += mapBox_SizeChanged;

            // 5. 定时器
            timerZoom.Interval = 10; timerZoom.Tick += TimerZoom_Tick;
            timerDownloadCheck.Interval = 500; timerDownloadCheck.Tick += (s, e) => { UpdateMap(); }; timerDownloadCheck.Start();

            // 6. TreeView1 (Map TOC) 初始化
            TreeNode baseNode = new TreeNode(basemapLayer.Name);
            baseNode.Tag = basemapLayer; baseNode.Checked = true;
            treeView1.Nodes.Add(baseNode);

            iconEyeOpen = Properties.Resources.icon_eye_open;
            iconEyeClose = Properties.Resources.icon_eye_close;
            treeView1.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            treeView1.ItemHeight = 26;
            treeView1.AllowDrop = true;

            // Map TreeView 事件
            treeView1.ItemDrag += treeView1_ItemDrag;
            treeView1.DragEnter += treeView1_DragEnter;
            treeView1.DragOver += treeView1_DragOver;
            treeView1.DragDrop += treeView1_DragDrop;
            treeView1.DragLeave += treeView1_DragLeave;
            treeView1.DrawNode += treeView1_DrawNode;
            treeView1.MouseDown += treeView1_MouseDown;
            treeView1.MouseMove += treeView1_MouseMove;
            treeView1.AfterCheck += treeView1_AfterCheck;

            // 7. 布局控件相关
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;

            // 初始化 Layout TreeView，并绑定事件监听
            InitLayoutTreeView();
            myLayoutControl.ElementChanged += (s, e) => { PopulateLayoutTree(); };
            myLayoutControl.SelectionChanged += (s, e) => { UpdateLayoutTreeSelection(); };

            // 更新初始视图
            view.Update(new XExtent(120, 125, 25, 35), mapBox.ClientRectangle);
        }

        // ==========================================
        // 【核心修改】Layout TreeView 逻辑
        // ==========================================
        private void InitLayoutTreeView()
        {
            treeViewLayout = new TreeView();
            treeViewLayout.Dock = DockStyle.Fill;
            treeViewLayout.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            treeViewLayout.ItemHeight = 26;
            treeViewLayout.Visible = false; // 初始隐藏
            treeViewLayout.CheckBoxes = false; // 自绘眼睛
            treeViewLayout.AllowDrop = true;   // 开启拖拽
            treeViewLayout.HideSelection = false; // 失去焦点时依然保持选中高亮

            if (treeView1.Parent != null)
            {
                treeView1.Parent.Controls.Add(treeViewLayout);
                treeViewLayout.BringToFront();
            }

            // 绑定 Layout 树专用事件
            treeViewLayout.DrawNode += treeView1_DrawNode; // 复用绘制逻辑(画眼睛和背景)
            treeViewLayout.MouseDown += TreeViewLayout_MouseDown; // 专门处理 Layout 下的点击
            treeViewLayout.ItemDrag += (s, e) => { DoDragDrop(e.Item, DragDropEffects.Move); };
            treeViewLayout.DragEnter += (s, e) => { e.Effect = DragDropEffects.Move; };
            treeViewLayout.DragOver += TreeViewLayout_DragOver;
            treeViewLayout.DragDrop += TreeViewLayout_DragDrop;
        }

        // 【关键】刷新 Layout 树 (根据 Page.Elements 生成)
        private void PopulateLayoutTree()
        {
            treeViewLayout.BeginUpdate();
            treeViewLayout.Nodes.Clear();

            // 注意：Elements 列表索引 0 是最底层（先画），索引 Count-1 是最顶层（后画）。
            // 在 TreeView 里，我们通常习惯把“顶层”的图层放在最上面。
            // 所以这里我们需要【倒序】遍历 Elements 添加到 TreeView。
            var elements = myLayoutControl.Page.Elements;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                XLayoutElement ele = elements[i];
                TreeNode node = new TreeNode(ele.Name);
                node.Tag = ele;
                node.Checked = ele.Visible; // 用 Checked 状态来存显隐
                treeViewLayout.Nodes.Add(node);

                // 如果该元素当前被选中，树节点也要选中
                if (ele.IsSelected)
                {
                    treeViewLayout.SelectedNode = node;
                }
            }
            treeViewLayout.EndUpdate();
        }

        // 单独更新树的选中状态 (不重建树)
        private void UpdateLayoutTreeSelection()
        {
            // 找到 Tag 等于 myLayoutControl.SelectedElement 的节点
            var selectedEle = myLayoutControl.SelectedElement;
            if (selectedEle == null)
            {
                treeViewLayout.SelectedNode = null;
                return;
            }

            foreach (TreeNode node in treeViewLayout.Nodes)
            {
                if (node.Tag == selectedEle)
                {
                    treeViewLayout.SelectedNode = node;
                    break;
                }
            }
        }

        // Layout 树点击事件 (处理开关、选中、右键)
        private void TreeViewLayout_MouseDown(object sender, MouseEventArgs e)
        {
            TreeViewHitTestInfo info = treeViewLayout.HitTest(e.Location);
            if (info.Node == null) return;
            TreeNode node = info.Node;
            XLayoutElement ele = node.Tag as XLayoutElement;
            if (ele == null) return;

            // 左键点击
            if (e.Button == MouseButtons.Left)
            {
                // 1. 检查是否点击了眼睛图标
                int diff = e.X - node.Bounds.X;
                if (diff >= 0 && diff <= 24)
                {
                    node.Checked = !node.Checked;
                    ele.Visible = node.Checked; // 同步数据
                    treeViewLayout.Invalidate();
                    myLayoutControl.Invalidate(); // 刷新 Layout 画布
                    return;
                }

                // 2. 普通选择：双向同步
                treeViewLayout.SelectedNode = node;
                myLayoutControl.SelectElement(ele); // 通知 LayoutControl 选中该元素
            }
            // 右键点击
            else if (e.Button == MouseButtons.Right)
            {
                treeViewLayout.SelectedNode = node;
                myLayoutControl.SelectElement(ele); // 右键也要选中

                // 创建 Layout 树专用的右键菜单
                ContextMenuStrip cms = new ContextMenuStrip();
                cms.Items.Add("删除元素", null, (s, args) => {
                    myLayoutControl.Page.Elements.Remove(ele);
                    myLayoutControl.Invalidate();
                    PopulateLayoutTree(); // 刷新树
                });

                // 如果是 MapFrame，可以加个“属性”
                if (ele is XMapFrame)
                {
                    cms.Items.Add("属性...", null, (s, args) => { MessageBox.Show("地图框属性功能开发中..."); });
                }

                cms.Show(treeViewLayout, e.Location);
            }
        }

        // Layout 树拖拽悬停
        private void TreeViewLayout_DragOver(object sender, DragEventArgs e)
        {
            Point pt = treeViewLayout.PointToClient(new Point(e.X, e.Y));
            TreeNode target = treeViewLayout.GetNodeAt(pt);
            if (dropTargetNode != target)
            {
                dropTargetNode = target;
                treeViewLayout.Invalidate();
            }
            e.Effect = DragDropEffects.Move;
        }

        // Layout 树拖拽放下 (处理 Z-Order)
        private void TreeViewLayout_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode srcNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
            Point pt = treeViewLayout.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeViewLayout.GetNodeAt(pt);

            dropTargetNode = null;
            treeViewLayout.Invalidate();

            if (srcNode == null || srcNode.TreeView != treeViewLayout) return; // 只允许 Layout 内部拖拽

            // 调整 TreeView 节点顺序
            if (targetNode == null)
            {
                treeViewLayout.Nodes.Remove(srcNode);
                treeViewLayout.Nodes.Add(srcNode);
            }
            else if (targetNode != srcNode)
            {
                treeViewLayout.Nodes.Remove(srcNode);
                treeViewLayout.Nodes.Insert(targetNode.Index, srcNode);
            }
            treeViewLayout.SelectedNode = srcNode;

            // =============================================
            // 【核心同步】根据 TreeView 顺序重构 Page.Elements 列表
            // =============================================

            // 记住：TreeView 最上面的节点 (Index 0) 应该是 Elements 列表的【最后一个】(最顶层)。
            // 所以我们要倒序把 TreeView 的 Tag 塞回 List。

            myLayoutControl.Page.Elements.Clear();
            for (int i = treeViewLayout.Nodes.Count - 1; i >= 0; i--)
            {
                XLayoutElement ele = treeViewLayout.Nodes[i].Tag as XLayoutElement;
                if (ele != null)
                {
                    myLayoutControl.Page.Elements.Add(ele);
                }
            }

            // 刷新 Layout 画布
            myLayoutControl.Invalidate();
        }

        // ==========================================
        // Tab 切换逻辑
        // ==========================================
        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Text == "Layout")
            {
                // 1. 显隐控制
                mapBox.Visible = false;
                myLayoutControl.Visible = true;

                treeView1.Visible = false;
                treeViewLayout.Visible = true; // 显示 Layout 专用树

                // 2. 更新 LayoutControl 数据
                XExtent currentExtent = view.CurrentMapExtent;
                if (currentExtent == null && layers.Count > 0) currentExtent = layers[0].Extent;

                if (myLayoutControl != null && currentExtent != null)
                {
                    myLayoutControl.UpdateLayout(layers, this.basemapLayer, currentExtent);
                }

                // 3. 刷新 Layout 树
                PopulateLayoutTree();
            }
            else // 切换回 Map
            {
                myLayoutControl.Visible = false;
                mapBox.Visible = true;

                treeViewLayout.Visible = false;
                treeView1.Visible = true;

                // 刷新 Map 视图
                if (mapBox.Width > 0 && mapBox.Height > 0)
                {
                    view.UpdateMapWindow(mapBox.ClientRectangle);
                    UpdateMap();
                }
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

            // 倒序绘制：树的下面先画，上面的后画（盖在上面）
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
            if (layers.Count == 1) button_FullExtent_Click(null, null);
            UpdateMap();
        }

        private void btnAddMapFrame_Click(object sender, EventArgs e) { SwitchToLayout(); myLayoutControl.StartCreateMapFrame(); }
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

            // 检查 sender 是否为当前绘制的控件的 dropTargetNode
            TreeNode target = (sender == treeView1) ? dropTargetNode : (sender == treeViewLayout ? dropTargetNode : null);
            if (target != null && e.Node == target)
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

            // 同步 layer 列表顺序
            layers.Clear();
            for (int i = treeView1.Nodes.Count - 1; i >= 0; i--)
            {
                if (treeView1.Nodes[i].Tag is XVectorLayer vl) layers.Add(vl);
            }

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
                if (diff >= 0 && diff <= 24)
                {
                    node.Checked = !node.Checked;
                    if (node.Tag is XVectorLayer vl) vl.Visible = node.Checked;
                    else if (node.Tag is XTileLayer tl) tl.Visible = node.Checked;

                    treeView1.Invalidate();
                    UpdateMap();
                    return;
                }
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

        // 菜单功能
        private void 注记ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = treeView1.SelectedNode?.Tag as XVectorLayer;
            if (l != null) { l.LabelOrNot = !l.LabelOrNot; UpdateMap(); }
        }

        private void 注记属性ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = treeView1.SelectedNode?.Tag as XVectorLayer;
            if (l != null) { if (new FormLabelProperty(l).ShowDialog() == DialogResult.OK) UpdateMap(); }
        }

        private void 移除图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = treeView1.SelectedNode?.Tag as XVectorLayer;
            if (l != null && MessageBox.Show("移除?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                layers.Remove(l);
                foreach (TreeNode n in treeView1.Nodes) { if (n.Tag == l) { treeView1.Nodes.Remove(n); break; } }
                UpdateMap();
                UpdateSelectionStatus();
            }
        }

        private void 缩放至图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = treeView1.SelectedNode?.Tag as XVectorLayer;
            if (l != null && l.Extent != null)
            {
                view.Update(l.Extent, mapBox.ClientRectangle);
                UpdateMap();
            }
        }

        private void 打开属性表ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = treeView1.SelectedNode?.Tag as XVectorLayer;
            if (l != null) new FormAttribute(l).Show();
        }
    }
}