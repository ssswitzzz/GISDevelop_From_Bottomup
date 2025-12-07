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
        // ==========================================
        // 核心数据
        // ==========================================
        List<XVectorLayer> layers = new List<XVectorLayer>();
        XView view = null;
        Bitmap backwindow;

        // ==========================================
        // 资源与工具
        // ==========================================
        Bitmap iconEyeOpen, iconEyeClose;
        XTileLayer basemapLayer;
        Timer timerDownloadCheck = new Timer();
        Timer timerZoom = new Timer();

        // ==========================================
        // 鼠标交互状态
        // ==========================================
        Point MouseDownLocation, MouseMovingLocation;
        XExploreActions currentMouseAction = XExploreActions.noaction;
        XExploreActions baseTool = XExploreActions.pan;

        // ==========================================
        // Layout 相关变量 (新增)
        // ==========================================
        // 专门用于 Layout 视图的图层树，与 Map 视图分离
        private TreeView treeViewLayout;
        private TreeNode dropTargetNode = null;
        Point TreeMouseDownLocation; // 记录树控件点击位置，用于判断拖拽

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

            // 7. 布局控件相关初始化
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;

            // 【核心】初始化 Layout 树
            InitLayoutTreeView();

            // 绑定 LayoutControl 的事件，实现双向同步
            myLayoutControl.ElementChanged += (s, e) => { PopulateLayoutTree(); };
            myLayoutControl.SelectionChanged += (s, e) => { UpdateLayoutTreeSelection(); };

            // 更新初始视图
            view.Update(new XExtent(120, 125, 25, 35), mapBox.ClientRectangle);
            InitButtonIcons();
        }
        private void InitButtonIcons()
        {
            // === 地图操作栏 (假设你在地图页面的按钮叫这些名字) ===
            // 根据 explore_button_Click 推测
            SetButtonStyle(explore_button, Properties.Resources.漫游);
            // 根据 btnSelect_Click 推测
            SetButtonStyle(btnSelect, Properties.Resources.选择);
            // 根据 button_FullExtent_Click 推测
            SetButtonStyle(button_FullExtent, Properties.Resources.全图显示);
            // 根据 button_ReadShp_Click 推测 (如果有图标的话)
            SetButtonStyle(button_ReadShp, Properties.Resources.shapefile); // 假设你有打开图标

            // === Layout 布局工具栏 (FlowLayoutPanel 里的按钮) ===
            // 这里的名字是你设计器里给按钮起的 (Name)

            SetButtonStyle(btnAddMapFrame, Properties.Resources.地图框);
            SetButtonStyle(btnAddNorthArrow, Properties.Resources.指北针);
            SetButtonStyle(btnAddScaleBar, Properties.Resources.比例尺);
            SetButtonStyle(btnAddLegend, Properties.Resources.图例);
            SetButtonStyle(btnAddText, Properties.Resources.文本框);
            SetButtonStyle(btnAddGrid, Properties.Resources.经纬网);
            SetButtonStyle(btnAddExport, Properties.Resources.导出地图);

            // 如果你在 Layout 页面也放了漫游和选择，记得把那两个按钮也加上
            // 例如：
            // SetButtonStyle(btnLayoutPan, Properties.Resources.漫游);
            // SetButtonStyle(btnLayoutSelect, Properties.Resources.选择);
        }
        // 专门用来给现有按钮“穿衣服”的辅助方法
        private void SetButtonStyle(Button btn, Image icon)
        {
            if (btn == null) return; // 防止按钮改名了找不到
                                     // 1. 【关键】强制断开设计器里的 ImageList 关联
                                     // 如果不加这句，代码里设置的 Image 大小可能会被设计器的设置覆盖
            btn.ImageList = null;
            int iconSize = 32;

            // 调用缩放方法，把大图变成小图标
            btn.Image = ResizeImage(icon, iconSize);


            // 2. 关键：设置图文关系为“图片在文字上方”
            btn.TextImageRelation = TextImageRelation.ImageAboveText;

            // 3. 对齐方式调整 (图片居中，文字在底部)
            btn.ImageAlign = ContentAlignment.BottomCenter;
            btn.TextAlign = ContentAlignment.BottomCenter;

            // 4. 美化外观 (扁平化，去边框)
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent; // 或者 Color.White

            // 5. 确保高度足够 (如果设计器里拉得太矮，图标和文字会重叠)
            // 如果你在设计器里已经调好了大小，这行可以注释掉
            if (btn.Height < 55) btn.Height = 60;
        }
        // 辅助方法：高质量调整图片大小
        private Image ResizeImage(Image imgToResize, int size)
        {
            // 获取原图的宽高
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            // 创建一个新的空画布，大小是你想要的 size (比如 32x32)
            Bitmap b = new Bitmap(size, size);

            // 创建画笔
            using (Graphics g = Graphics.FromImage((Image)b))
            {
                // 设置高质量插值法，防止缩放后变模糊
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                // 把原图画到新的小画布上
                g.DrawImage(imgToResize, 0, 0, size, size);
            }
            return (Image)b;
        }

        // ==========================================
        // 【核心部分】Layout TreeView 逻辑
        // ==========================================
        private void InitLayoutTreeView()
        {
            // 1. 动态创建一个新树
            treeViewLayout = new TreeView();
            treeViewLayout.Dock = DockStyle.Fill;
            treeViewLayout.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            treeViewLayout.ItemHeight = 26;
            treeViewLayout.Visible = false; // 初始隐藏
            treeViewLayout.CheckBoxes = false; // 自绘眼睛
            treeViewLayout.AllowDrop = true;   // 开启拖拽
            treeViewLayout.HideSelection = false; // 失去焦点依然高亮

            // 2. 将其添加到界面 (覆盖在 treeView1 上面)
            if (treeView1.Parent != null)
            {
                treeView1.Parent.Controls.Add(treeViewLayout);
                treeViewLayout.BringToFront();
            }

            // 3. 【重点】手动订阅事件

            // 复用绘制逻辑(画眼睛和背景)
            treeViewLayout.DrawNode += treeView1_DrawNode;

            // 专门处理点击 (实现整行选中、开关显隐、右键)
            treeViewLayout.MouseDown += TreeViewLayout_MouseDown;

            // 【关键】专门处理移动 (实现整行拖拽)
            treeViewLayout.MouseMove += TreeViewLayout_MouseMove;

            // 拖拽相关
            treeViewLayout.ItemDrag += (s, e) => { DoDragDrop(e.Item, DragDropEffects.Move); };
            treeViewLayout.DragEnter += (s, e) => { e.Effect = DragDropEffects.Move; };
            treeViewLayout.DragOver += TreeViewLayout_DragOver;
            treeViewLayout.DragDrop += TreeViewLayout_DragDrop;
        }

        // 刷新 Layout 树 (根据 Page.Elements 生成)
        private void PopulateLayoutTree()
        {
            treeViewLayout.BeginUpdate();
            treeViewLayout.Nodes.Clear();

            // 倒序遍历，保证顶层元素在树的最上方
            var elements = myLayoutControl.Page.Elements;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                XLayoutElement ele = elements[i];
                TreeNode node = new TreeNode(ele.Name);
                node.Tag = ele;
                node.Checked = ele.Visible; // Checked 借用来存显隐状态
                treeViewLayout.Nodes.Add(node);

                if (ele.IsSelected)
                {
                    treeViewLayout.SelectedNode = node;
                }
            }
            treeViewLayout.EndUpdate();
        }

        // 仅更新树的选中状态
        private void UpdateLayoutTreeSelection()
        {
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

        // Layout 树点击事件
        private void TreeViewLayout_MouseDown(object sender, MouseEventArgs e)
        {
            // 【技巧】使用 (0, e.Y) 强制获取当前行，不管点的是字还是空白
            TreeNode node = treeViewLayout.GetNodeAt(0, e.Y);
            if (node == null) return;

            XLayoutElement ele = node.Tag as XLayoutElement;
            if (ele == null) return;

            // 记录位置供 MouseMove 拖拽判断
            TreeMouseDownLocation = e.Location;

            if (e.Button == MouseButtons.Left)
            {
                // 1. 判断是否点击眼睛 (假设眼睛区域在文字左侧 30px 内)
                if (e.X < node.Bounds.X)
                {
                    node.Checked = !node.Checked;
                    ele.Visible = node.Checked; // 同步 Visible

                    treeViewLayout.Invalidate();
                    myLayoutControl.Refresh(); // 【修改】强制立即刷新
                    return;
                }

                // 2. 整行选中
                treeViewLayout.SelectedNode = node;
                myLayoutControl.SelectElement(ele); // 通知画布选中
            }
            else if (e.Button == MouseButtons.Right)
            {
                treeViewLayout.SelectedNode = node;
                myLayoutControl.SelectElement(ele);

                // 右键菜单
                ContextMenuStrip cms = new ContextMenuStrip();
                cms.Items.Add("删除元素", null, (s, args) => {
                    myLayoutControl.Page.Elements.Remove(ele);
                    myLayoutControl.Invalidate();
                    PopulateLayoutTree();
                });
                if (ele is XMapFrame)
                {
                    cms.Items.Add("属性...", null, (s, args) => { MessageBox.Show("地图框属性功能..."); });
                }
                cms.Show(treeViewLayout, e.Location);
            }
        }

        // 【关键新增】Layout 树鼠标移动事件 (手动触发拖拽)
        private void TreeViewLayout_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || treeViewLayout.SelectedNode == null) return;

            // 判断拖拽距离阈值
            Size dragSize = SystemInformation.DragSize;
            Rectangle dragRect = new Rectangle(
                new Point(TreeMouseDownLocation.X - dragSize.Width / 2, TreeMouseDownLocation.Y - dragSize.Height / 2),
                dragSize);

            // 如果鼠标移出了阈值区域，则开始拖拽
            if (!dragRect.Contains(e.Location))
            {
                // 手动启动 DragDrop，这样点整行任意位置都能拖
                DoDragDrop(treeViewLayout.SelectedNode, DragDropEffects.Move);
            }
        }

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
        // 【关键修复点】
        private void TreeViewLayout_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode srcNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
            Point pt = treeViewLayout.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeViewLayout.GetNodeAt(pt);

            dropTargetNode = null;
            treeViewLayout.Invalidate();

            if (srcNode == null || srcNode.TreeView != treeViewLayout) return;

            // 1. 调整 TreeView 中的节点顺序
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

            // 2. 根据 TreeView 的新顺序，重构 myLayoutControl.Page.Elements 列表
            // 逻辑：TreeView 最上面的节点 (Index 0) 对应最顶层 (Elements 的最后一个)
            // 列表通常是：0 (最底层) -> Count-1 (最顶层)
            // 所以我们倒序遍历 TreeView，把节点加到 List 里
            myLayoutControl.Page.Elements.Clear();

            for (int i = treeViewLayout.Nodes.Count - 1; i >= 0; i--)
            {
                XLayoutElement ele = treeViewLayout.Nodes[i].Tag as XLayoutElement;
                if (ele != null) myLayoutControl.Page.Elements.Add(ele);
            }

            // 3. 【核心修复】强制同步重绘
            // 之前用 Invalidate() 可能会延迟，导致松手瞬间看不到变化
            // 改用 Refresh() 立即执行 Paint
            myLayoutControl.Refresh();
        }

        // ==========================================
        // Tab 切换逻辑
        // ==========================================
        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Text == "Layout")
            {
                // 切换到 Layout
                mapBox.Visible = false;
                myLayoutControl.Visible = true;
                treeView1.Visible = false;
                treeViewLayout.Visible = true;

                // 更新 LayoutControl 数据
                XExtent currentExtent = view.CurrentMapExtent;
                if (currentExtent == null && layers.Count > 0) currentExtent = layers[0].Extent;

                if (myLayoutControl != null && currentExtent != null)
                {
                    myLayoutControl.UpdateLayout(layers, this.basemapLayer, currentExtent);
                }

                PopulateLayoutTree();
            }
            else
            {
                // 切换回 Map
                myLayoutControl.Visible = false;
                mapBox.Visible = true;
                treeViewLayout.Visible = false;
                treeView1.Visible = true;

                if (mapBox.Width > 0 && mapBox.Height > 0)
                {
                    view.UpdateMapWindow(mapBox.ClientRectangle);
                    UpdateMap();
                }
            }
        }

        // ==========================================
        // Map 视图原有逻辑 (保持不变)
        // ==========================================
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

        // 布局工具按钮事件
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

        // treeView1 的绘制和事件 (Layout树也复用了一部分)
        private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Color backColor = ((e.State & TreeNodeStates.Selected) != 0) ? Color.FromArgb(204, 232, 255) : Color.White;
            using (SolidBrush brush = new SolidBrush(backColor)) e.Graphics.FillRectangle(brush, e.Bounds);

            Rectangle imgRect = new Rectangle(e.Bounds.X + 2, e.Bounds.Y, 30, 30);
            Image imgToDraw = e.Node.Checked ? iconEyeOpen : iconEyeClose;
            if (imgToDraw != null) e.Graphics.DrawImage(imgToDraw, imgRect);

            Rectangle textRect = new Rectangle(e.Bounds.X + 30, e.Bounds.Y, e.Bounds.Width - 30, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Node.Text, ((TreeView)sender).Font, textRect, Color.Black, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            TreeNode target = (sender == treeView1) ? dropTargetNode : (sender == treeViewLayout ? dropTargetNode : null);
            if (target != null && e.Node == target)
            {
                using (Pen linePen = new Pen(Color.FromArgb(0, 122, 204), 3))
                    e.Graphics.DrawLine(linePen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right, e.Bounds.Top);
            }
        }

        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e) { currentMouseAction = XExploreActions.noaction; DoDragDrop(e.Item, DragDropEffects.Move); }
        private void treeView1_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Move; }
        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e) { UpdateMap(); }

        private void treeView1_DragOver(object sender, DragEventArgs e)
        {
            Point pt = treeView1.PointToClient(new Point(e.X, e.Y));
            TreeNode target = treeView1.GetNodeAt(pt);
            if (dropTargetNode != target) { dropTargetNode = target; treeView1.Invalidate(); }
            e.Effect = DragDropEffects.Move;
        }

        // 地图图层拖拽 (如果你也想让地图图层拖拽时，Layout界面如果有地图框也能实时更新，这里也加了 Refresh)
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

            layers.Clear();
            for (int i = treeView1.Nodes.Count - 1; i >= 0; i--)
            {
                if (treeView1.Nodes[i].Tag is XVectorLayer vl) layers.Add(vl);
            }
            UpdateMap();
            myLayoutControl.Refresh(); // 【修改】这里也改成了 Refresh，防止切换回 Layout 时显示旧图像
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
                if (diff >= -30 && diff <= 0)
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

        // 右键菜单
        private void 注记ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = null;
            if (tabControl1.SelectedTab.Text == "Layout" && treeViewLayout.SelectedNode != null) l = treeViewLayout.SelectedNode.Tag as XVectorLayer;
            else if (treeView1.SelectedNode != null) l = treeView1.SelectedNode.Tag as XVectorLayer;
            if (l != null) { l.LabelOrNot = !l.LabelOrNot; UpdateMap(); myLayoutControl.Invalidate(); }
        }

        private void 注记属性ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = null;
            if (tabControl1.SelectedTab.Text == "Layout" && treeViewLayout.SelectedNode != null) l = treeViewLayout.SelectedNode.Tag as XVectorLayer;
            else if (treeView1.SelectedNode != null) l = treeView1.SelectedNode.Tag as XVectorLayer;
            if (l != null) { if (new FormLabelProperty(l).ShowDialog() == DialogResult.OK) { UpdateMap(); myLayoutControl.Invalidate(); } }
        }

        private void 移除图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = null;
            TreeNode nodeToRemove = null;

            if (tabControl1.SelectedTab.Text == "Layout") { nodeToRemove = treeViewLayout.SelectedNode; l = nodeToRemove?.Tag as XVectorLayer; }
            else { nodeToRemove = treeView1.SelectedNode; l = nodeToRemove?.Tag as XVectorLayer; }

            if (l != null && MessageBox.Show("移除?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                layers.Remove(l);
                foreach (TreeNode n in treeView1.Nodes) { if (n.Tag == l) { treeView1.Nodes.Remove(n); break; } }
                if (tabControl1.SelectedTab.Text == "Layout") treeViewLayout.Nodes.Remove(nodeToRemove);
                UpdateMap();
                UpdateSelectionStatus();
                myLayoutControl.Invalidate();
            }
        }

        private void 缩放至图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = null;
            if (tabControl1.SelectedTab.Text == "Layout" && treeViewLayout.SelectedNode != null) l = treeViewLayout.SelectedNode.Tag as XVectorLayer;
            else if (treeView1.SelectedNode != null) l = treeView1.SelectedNode.Tag as XVectorLayer;
            if (l != null && l.Extent != null) { view.Update(l.Extent, mapBox.ClientRectangle); UpdateMap(); myLayoutControl.Invalidate(); }
        }

        private void 符号系统ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 获取当前选中的图层
            XVectorLayer layer = null;
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Tag is XVectorLayer)
            {
                layer = treeView1.SelectedNode.Tag as XVectorLayer;
            }

            // Layout 视图的判断逻辑
            if (layer == null && tabControl1.SelectedTab == tabPage2 && treeViewLayout.SelectedNode != null)
            {
                layer = treeViewLayout.SelectedNode.Tag as XVectorLayer;
            }

            if (layer == null) return;

            // 打开符号设置窗口
            FormSymbology frm = new FormSymbology(layer);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                UpdateMap(); // 刷新地图
                if (myLayoutControl.Visible) myLayoutControl.Invalidate(); // 刷新布局
            }

        }

        private void btnAddLegend_Click(object sender, EventArgs e)
        {
            myLayoutControl.StartCreateLegend();
        }

        private void btnAddExport_Click(object sender, EventArgs e)
        {
            // 1. 弹出设置窗口
            FormExport frm = new FormExport();
            if (frm.ShowDialog() == DialogResult.OK)
            {
                // 2. 弹出保存文件窗口
                SaveFileDialog sfd = new SaveFileDialog();
                string ext = frm.ExportFormat.ToString().ToLower();
                if (ext == "jpeg") ext = "jpg";

                sfd.Filter = $"{ext.ToUpper()} File|*.{ext}";
                sfd.FileName = $"Map_Export_{DateTime.Now:MMdd_HHmm}";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 3. 开始导出
                        // 鼠标变漏斗，防止用户乱点
                        this.Cursor = Cursors.WaitCursor;

                        myLayoutControl.ExportToImage(sfd.FileName, frm.ExportDPI, frm.ExportFormat, frm.ExportQuality);

                        this.Cursor = Cursors.Default;
                        MessageBox.Show("导出成功！\n文件保存在: " + sfd.FileName, "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // 可选：导出完自动打开图片
                        System.Diagnostics.Process.Start(sfd.FileName);
                    }
                    catch (Exception ex)
                    {
                        this.Cursor = Cursors.Default;
                        MessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void 打开属性表ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XVectorLayer l = null;
            if (tabControl1.SelectedTab.Text == "Layout" && treeViewLayout.SelectedNode != null) l = treeViewLayout.SelectedNode.Tag as XVectorLayer;
            else if (treeView1.SelectedNode != null) l = treeView1.SelectedNode.Tag as XVectorLayer;
            if (l != null) new FormAttribute(l).Show();
        }
        // 这就是你按钮点击事件里唯一需要写的代码
        private void btnAddText_Click(object sender, EventArgs e)
        {
            // 1. 如果不在 Layout 页面，先切过去（可选）
            if (tabControl1.SelectedTab != tabPage2)
                tabControl1.SelectedTab = tabPage2;

            // 2. 告诉 LayoutControl：准备开始画文字！
            myLayoutControl.StartCreateText();
        }
    }
}