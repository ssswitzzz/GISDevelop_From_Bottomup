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
        // 注意：layers 列表现在只起辅助作用，核心顺序以 treeView1.Nodes 为准
        List<XVectorLayer> layers = new List<XVectorLayer>();
        XView view = null;
        Bitmap backwindow;
        Bitmap iconEyeOpen;
        Bitmap iconEyeClose;
        XTileLayer basemapLayer; // 新增底图变量
        // 我们需要一个定时器来不断刷新正在下载的底图
        Timer timerDownloadCheck = new Timer();


        // 交互状态变量
        Point MouseDownLocation, MouseMovingLocation;
        XExploreActions currentMouseAction = XExploreActions.noaction;
        private TreeNode dropTargetNode = null;
        Timer timerZoom = new Timer();
        // 1. 新增：记录当前激活的工具（默认是漫游）
        XExploreActions baseTool = XExploreActions.pan;
        public FormMap()
        {
            InitializeComponent();
            // 1. 网络协议设置 (天地图也需要)
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;


            // 2. 初始化 View (记得要用 -85 到 85，防止卡死！)
            view = new XView(new XExtent(-180, 180, -85, 85), mapBox.ClientRectangle);

            // 3. 【修改】使用天地图 URL
            // T=vec_w 表示矢量底图(Web墨卡托)，兼容我们的算法
            // 注意：把下面的 "你的Key" 换成你刚才申请到的真实 Key！
            string myKey = "5e531967100311fcb8098b759848b71c";
            string url = $"https://t0.tianditu.gov.cn/DataServer?T=vec_w&x={{x}}&y={{y}}&l={{z}}&tk={myKey}";

            basemapLayer = new XTileLayer(url);
            basemapLayer.Name = "天地图矢量";



            mapBox.MouseWheel += mapBox_MouseWheel;
            mapBox.MouseLeave += MapBox_MouseLeave;
            timerZoom.Interval = 10; // 15毫秒刷新一次，约等于 60 FPS
            timerZoom.Tick += TimerZoom_Tick; // 绑定事件


            // 2. 把底图加到 TreeView 的最底部
            TreeNode baseNode = new TreeNode(basemapLayer.Name);
            baseNode.Tag = basemapLayer; // 绑定对象
            baseNode.Checked = true;
            treeView1.Nodes.Add(baseNode); // Add 是加到末尾，也就是最底层

            // 3. 设置下载刷新定时器
            // 因为下载是异步的，下载完后需要通知界面重绘。简单做法是用个 Timer 每 0.5 秒刷一下
            timerDownloadCheck.Interval = 500;
            timerDownloadCheck.Tick += (s, e) => { UpdateMap(); };
            timerDownloadCheck.Start();

            // 4. 初始化视图为全球范围 (经纬度)
            view.Update(new XExtent(120, 125, 25, 35), mapBox.ClientRectangle);


            List<XVectorLayer> layers = new List<XVectorLayer>();

            iconEyeOpen = Properties.Resources.icon_eye_open;
            iconEyeClose = Properties.Resources.icon_eye_close;
            // 【关键修改】开启完全自绘模式，这样 Checkbox 会消失，由我们自己画
            treeView1.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            treeView1.ItemHeight = 26; // 稍微调高一点，让行距不那么挤，更有现代感

            // 绑定事件（你原本有的）
            treeView1.ItemDrag += treeView1_ItemDrag;
            treeView1.DragEnter += treeView1_DragEnter;
            treeView1.DragOver += treeView1_DragOver; // 记得绑定 DragOver
            treeView1.DragDrop += treeView1_DragDrop;
            treeView1.DragLeave += treeView1_DragLeave;
            treeView1.DrawNode += treeView1_DrawNode;
            treeView1.MouseDown += treeView1_MouseDown; // 新增：处理点击眼睛图标
            treeView1.MouseMove += treeView1_MouseMove;





        }
        private void UpdateSelectionStatus()
        {
            int totalCount = 0;
            foreach (TreeNode node in treeView1.Nodes)
            {
                if (node.Tag is XVectorLayer layer)
                {
                    totalCount += layer.SelectedFeatures.Count;
                }
            }

            lblSelectCount.Text = $"选中要素: {totalCount}";
        }

        private void MapBox_MouseLeave(object sender, EventArgs e)
        {
            lblCoordinates.Text = "Ready";
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
            // 【关键算法】画家算法：从下往上画
            // TreeView 的 Nodes[0] 是最顶层，Nodes[Count-1] 是最底层
            // 所以我们要倒序遍历 TreeView 的节点
            for (int i = treeView1.Nodes.Count - 1; i >= 0; i--)
            {
                TreeNode node = treeView1.Nodes[i];
                if (!node.Checked) continue;

                // 【关键修改】判断节点里的 Tag 是哪种图层
                if (node.Tag is XVectorLayer vectorLayer)
                {
                    // 是矢量图层，用原来的画法
                    vectorLayer.draw(g, view);
                }
                else if (node.Tag is XTileLayer tileLayer)
                {
                    // 是底图图层，用底图画法
                    tileLayer.Draw(g, view);
                }
            }

            g.Dispose();

            // 4. 通知 mapBox 重绘自己 (触发 mapBox_Paint)
            mapBox.Invalidate();
        }

        private void mapBox_Paint(object sender, PaintEventArgs e)
        {
            if (backwindow == null) return;
            if (currentMouseAction == XExploreActions.pan)
            {
                e.Graphics.DrawImage(backwindow,
                    MouseMovingLocation.X - MouseDownLocation.X,
                    MouseMovingLocation.Y - MouseDownLocation.Y);
            }
            // 情况 B：其他所有情况（静止、框选、点选）-> 画在原点 (0,0)
            // 【关键】这一步绝对不能少，否则松开鼠标地图就消失了！
            else
            {
                e.Graphics.DrawImage(backwindow, 0, 0);

                // 3. 只有在画在原点的情况下，才考虑要不要画上面的红框/蓝框
                // 这样可以避免漫游时画框，也能保证静止时能看到图
                if (currentMouseAction == XExploreActions.zoominbybox ||
                    currentMouseAction == XExploreActions.select)
                {
                    // 计算框的参数
                    int x = Math.Min(MouseDownLocation.X, MouseMovingLocation.X);
                    int y = Math.Min(MouseDownLocation.Y, MouseMovingLocation.Y);
                    int width = Math.Abs(MouseDownLocation.X - MouseMovingLocation.X);
                    int height = Math.Abs(MouseDownLocation.Y - MouseMovingLocation.Y);

                    // 只有当鼠标真的动了（宽高大于0）才画框，避免点选时出现小杂点
                    if (width > 0 && height > 0)
                    {
                        Color boxColor = (currentMouseAction == XExploreActions.select) ? Color.Blue : Color.Red;
                        // 使用 Pen 而不是 Brush，画空心框
                        using (Pen pen = new Pen(boxColor, 2))
                        {
                            e.Graphics.DrawRectangle(pen, x, y, width, height);
                        }
                    }
                }
            }
        }

        private void mapBox_MouseDown(object sender, MouseEventArgs e)
        {
            // 1. 【解决问题2】屏蔽右键：如果不是左键或中键，直接返回
            if (e.Button == MouseButtons.Right) return;

            MouseDownLocation = e.Location;

            // 2. 处理 Shift 拉框放大 (优先级最高)
            if (Control.ModifierKeys == Keys.Shift)
            {
                currentMouseAction = XExploreActions.zoominbybox;
                return; // 结束，不再判断后面的
            }

            // 3. 【解决问题2】中键逻辑：无论当前是什么工具，按中键强制漫游
            if (e.Button == MouseButtons.Middle)
            {
                currentMouseAction = XExploreActions.pan;
                mapBox.Cursor = Cursors.Hand; // 临时变手
                return;
            }

            // 4. 左键逻辑：根据当前选中的工具 (baseTool) 决定动作
            if (e.Button == MouseButtons.Left)
            {
                // 如果当前工具是选择，那就开始选择
                if (baseTool == XExploreActions.select)
                {
                    currentMouseAction = XExploreActions.select;
                }
                // 如果当前工具是漫游，那就开始漫游
                else if (baseTool == XExploreActions.pan)
                {
                    currentMouseAction = XExploreActions.pan;
                }
            }
        }

        private void mapBox_MouseMove(object sender, MouseEventArgs e)
        {
            // 1. 显示坐标 (假设你底部的 Label 叫 lblCoordinates)
            XVertex mapVertex = view.ToMapVertex(e.Location);
            // 这里记得把 labelXY 换成你 StatusStrip 上那个 Label 的名字
            lblCoordinates.Text = $"X: {mapVertex.x:F2}, Y: {mapVertex.y:F2}";

            // 【解决问题3】关键检查：如果当前没有动作（鼠标没按下），直接退出！
            // 这样就不会出现“鼠标一动地图就跑”的灵异现象了
            if (currentMouseAction == XExploreActions.noaction) return;

            // 记录移动位置
            MouseMovingLocation = e.Location;

            // 只有在真正操作时才重绘
            if (currentMouseAction == XExploreActions.pan ||
                currentMouseAction == XExploreActions.zoominbybox ||
                currentMouseAction == XExploreActions.select)
            {
                mapBox.Invalidate();
            }
        }


        private void mapBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (MouseDownLocation == e.Location && currentMouseAction != XExploreActions.select)
            {
                currentMouseAction = XExploreActions.noaction;
                return;
            }

            XVertex v1 = view.ToMapVertex(MouseDownLocation);
            XVertex v2 = view.ToMapVertex(e.Location);

            // 1. 处理拉框放大 (保持原有逻辑)
            if (currentMouseAction == XExploreActions.zoominbybox)
            {
                // ... 原有放大逻辑 ...
                if (Math.Abs(MouseDownLocation.X - e.X) > 2) // 防止误触
                {
                    XExtent extent = new XExtent(v1, v2);
                    view.Update(extent, mapBox.ClientRectangle);
                }
            }
            // 2. 处理漫游 (保持原有逻辑)
            else if (currentMouseAction == XExploreActions.pan)
            {
                view.OffsetCenter(v1, v2);
            }
            // 3. 【新增】处理选择逻辑
            else if (currentMouseAction == XExploreActions.select)
            {
                // 计算屏幕上的拖拽距离
                int dx = Math.Abs(MouseDownLocation.X - e.X);
                int dy = Math.Abs(MouseDownLocation.Y - e.Y);

                // 如果按住了 Ctrl 键，表示“追加选择” (Modify = true)
                // 否则是“新选择” (Modify = false，会清空之前的)
                bool modify = (Control.ModifierKeys == Keys.Control);

                // 判断是“点选”还是“框选” (阈值设为 5 像素)
                if (dx < 5 && dy < 5)
                {
                    // === 点选模式 ===
                    // 将屏幕上的 5 像素转化为地图上的实际距离作为容差
                    double tolerance = view.ToMapDistance(5);

                    // 倒序遍历图层（优先选择最上面的图层）
                    // 注意：这里我们只处理 VectorLayer，跳过 TileLayer
                    for (int i = 0; i < treeView1.Nodes.Count; i++)
                    {
                        TreeNode node = treeView1.Nodes[i];
                        if (node.Checked && node.Tag is XVectorLayer layer)
                        {
                            layer.SelectByVertex(v1, tolerance, modify);
                            // 如果只想选中最上层的一个，可以在这里 break;
                            // 如果想“穿透选择”，就不要 break;
                        }
                    }
                }
                else
                {
                    // === 框选模式 ===
                    XExtent selectExtent = new XExtent(v1, v2);

                    foreach (TreeNode node in treeView1.Nodes)
                    {
                        if (node.Checked && node.Tag is XVectorLayer layer)
                        {
                            layer.SelectByExtent(selectExtent, modify);
                        }
                    }
                }

                // 更新状态栏显示的选中数量
                UpdateSelectionStatus();
            }

            currentMouseAction = XExploreActions.noaction;

            // 恢复光标样式 (根据 baseTool 恢复)
            if (baseTool == XExploreActions.pan)
                mapBox.Cursor = Cursors.Hand;
            else
                mapBox.Cursor = Cursors.Default;
            UpdateMap(); // 动作结束，生成新图
        }

        // 鼠标滚轮缩放
        private void mapBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
                view.SetZoomTarget(e.Location, true);
            else
                view.SetZoomTarget(e.Location, false);
            // 2. 启动定时器，开始动画
            if (!timerZoom.Enabled)
            {
                timerZoom.Start();
            }

            UpdateMap();
        }

        // 窗口大小改变时，地图也要重绘
        private void mapBox_SizeChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }
        private void TimerZoom_Tick(object sender, EventArgs e)
        {
            // 1. 让 View 往前走一步
            bool finished = view.UpdateBuffer();

            // 2. 刷新画面
            UpdateMap();

            // 3. 如果已经到了目标位置
            if (finished)
            {
                timerZoom.Stop();

                // 【新增这行】动画结束了，把目标清空。
                // 这样如果你接下来进行平移操作，下次缩放时会基于新的平移位置重新生成目标，不会乱跳。
                view.TargetExtent = null;
            }
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
            baseTool = XExploreActions.pan; // 只记录“我现在想用漫游工具”
            currentMouseAction = XExploreActions.noaction; // 此时还没按下鼠标，所以是无动作
            mapBox.Cursor = Cursors.Hand;
        }

        private void button_ReadShp_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Shapefile|*.shp"; 
            if (dialog.ShowDialog() != DialogResult.OK) return;
            foreach (string filename in dialog.FileNames)
            {
                XVectorLayer newLayer = XShapefile.ReadShapefile(filename);
                newLayer.Name = System.IO.Path.GetFileNameWithoutExtension(filename);
                newLayer.LabelOrNot = false;
                // 创建节点
                TreeNode node = new TreeNode(newLayer.Name);
                node.Checked = true; // 默认打钩
                node.Tag = newLayer; // 【关键】把数据绑定在节点上！

                // 把新图层插到最上面 (Index 0)
                treeView1.Nodes.Insert(0, node);

                // 同时也加到列表里备份（可选，主要为了方便计算全图）
                layers.Add(newLayer);
            }

            // 只有当这是第一次加载数据时，才自动缩放全图
            // 这样就不会出现加载第二个图层时，视野突然乱跳的问题
            if (layers.Count == 1) // 或者判断 treeView1.Nodes.Count == 1
            {
                // 调用下面的全图逻辑
                button_FullExtent_Click(null, null);
            }
            UpdateMap();
        }

        private void button_FullExtent_Click(object sender, EventArgs e)
        {
            if (treeView1.Nodes.Count == 0) return;
            XExtent fullExtent = null;
            foreach (TreeNode node in treeView1.Nodes)
            {
                if (node.Tag is XVectorLayer layer)
                {
                    // 如果图层本身没范围（比如空图层），跳过
                    if (layer.Extent == null) continue;

                    if (fullExtent == null)
                        fullExtent = new XExtent(layer.Extent);
                    else
                        fullExtent.Merge(layer.Extent);
                }
            }

            // 2. 更新视图
            if (fullExtent != null)
            {
                view.Update(fullExtent, mapBox.ClientRectangle);
                UpdateMap();
            }
        }


        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // 强制清除地图操作状态，防止出现蓝框
            currentMouseAction = XExploreActions.noaction;
            // 开始拖动选中的节点
            DoDragDrop(e.Item, DragDropEffects.Move);
        }
        private void btnSelect_Click(object sender, EventArgs e)
        {
            baseTool = XExploreActions.select; // 只记录“我现在想用选择工具”
            currentMouseAction = XExploreActions.noaction;
            // 鼠标变成箭头（通常选择模式用默认箭头）
            mapBox.Cursor = Cursors.Default;
        }



        // -------------------------------
        // TreeView界面

        // 拖拽进入区域
        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            // 允许移动操作
            e.Effect = DragDropEffects.Move;
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {

            UpdateMap();
        }

        private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            // 1. 开启抗锯齿，让文字和线条平滑
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 2. 绘制背景
            // 选中状态用淡蓝色，未选中用白色
            Color backColor = ((e.State & TreeNodeStates.Selected) != 0) ?
                              Color.FromArgb(204, 232, 255) : Color.White;
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // --- 核心修改：定义严格的区域 ---
            int iconWidth = 30; // 眼睛图标的点击宽度
            int clickLimit = 24;
            // 1. 计算眼睛的位置 (在 Bounds 的最左侧，居中)
            // 假设图片是 20x20
            int imgSize = 30;
            int imgY = e.Bounds.Y + (treeView1.ItemHeight - imgSize) / 2;
            // 让图片在 iconWidth 区域内居中
            int imgX = e.Bounds.X + (iconWidth - imgSize) / 2;

            Rectangle imgRect = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 4, 20, 20);

            // 画图标
            bool isVisible = e.Node.Checked;
            Image imgToDraw = isVisible ? iconEyeOpen : iconEyeClose;
            if (imgToDraw != null) e.Graphics.DrawImage(imgToDraw, imgRect);

            int textOffset = 30; // 只要这个数字比 clickLimit 大，点文字就绝不会触发！

            Rectangle textRect = new Rectangle(
                e.Bounds.X + textOffset,  // 从 X + 30 开始画文字
                e.Bounds.Y,
                e.Bounds.Width - textOffset,
                e.Bounds.Height);

            TextRenderer.DrawText(e.Graphics, e.Node.Text, treeView1.Font,
                textRect, Color.Black, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            // 5. 【关键】绘制拖拽指示线
            // 现在的逻辑是：拖拽到目标节点时，默认插入到它上面。所以我们在目标节点顶部画一根线。
            if (dropTargetNode != null && e.Node == dropTargetNode)
            {
                // 画一根显眼的蓝线或红线，带一点圆头，像 ArcGIS Pro 那样
                using (Pen linePen = new Pen(Color.FromArgb(0, 122, 204), 3))
                {
                    e.Graphics.DrawLine(linePen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right, e.Bounds.Top);
                }
            }
        }

        // =================================================================
        // 2. 拖拽逻辑 (带视觉反馈)
        // =================================================================
        private void treeView1_DragOver(object sender, DragEventArgs e)
        {
            Point pt = treeView1.PointToClient(new Point(e.X, e.Y));
            TreeNode target = treeView1.GetNodeAt(pt);

            // 只有目标变了才重绘，防止闪烁
            if (dropTargetNode != target)
            {
                dropTargetNode = target;
                treeView1.Invalidate(); // 这一句会让 DrawNode 重新运行，画出横线
            }

            e.Effect = DragDropEffects.Move;

            // 如果需要，这里可以加一段代码：当拖拽到边缘时自动滚动 TreeView
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            // 1. 获取源节点
            TreeNode srcNode = (TreeNode)e.Data.GetData(typeof(TreeNode));

            // 2. 获取目标节点 (就是我们刚才画线的那个位置)
            Point pt = treeView1.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeView1.GetNodeAt(pt);

            // 清除指示线
            dropTargetNode = null;
            treeView1.Invalidate();

            if (srcNode == null) return;

            // 3. 移动节点逻辑
            if (targetNode == null)
            {
                // 如果拖到了空白处，默认移动到最底部
                treeView1.Nodes.Remove(srcNode);
                treeView1.Nodes.Add(srcNode);
            }
            else if (targetNode != srcNode)
            {
                // 移动到目标节点之前 (Insert)
                treeView1.Nodes.Remove(srcNode);
                treeView1.Nodes.Insert(targetNode.Index, srcNode);
            }

            // 4. 选中并刷新地图
            treeView1.SelectedNode = srcNode;
            UpdateMap();
        }

        // 离开 TreeView 时清除线条
        private void treeView1_DragLeave(object sender, EventArgs e)
        {
            dropTargetNode = null;
            treeView1.Invalidate();
        }

        Point TreeMouseDownLocation;
        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            // 使用 HitTest 获取点击位置的节点信息
            TreeViewHitTestInfo info = treeView1.HitTest(e.Location);
            if (info.Node == null) return;

            TreeNode node = info.Node;

            // === 现有的左键逻辑 (保持不变) ===
            if (e.Button == MouseButtons.Left)
            {
                TreeMouseDownLocation = e.Location;
                int diff = e.X - node.Bounds.X;

                // 眼睛开关逻辑
                if (diff >= 0 && diff <= 24)
                {
                    node.Checked = !node.Checked;
                    treeView1.Invalidate();
                    UpdateMap();
                    return;
                }

                treeView1.SelectedNode = node;
            }

            // === 【新增】右键逻辑 ===
            else if (e.Button == MouseButtons.Right)
            {
                // 1. 强制选中当前右键点击的节点 (符合 Windows 操作习惯)
                treeView1.SelectedNode = node;

                // 2. 判断该节点是不是矢量图层 (XVectorLayer)
                // 只有矢量图层才有属性表，底图(XTileLayer)没有
                if (node.Tag is XVectorLayer)
                {
                    // 3. 在鼠标位置显示右键菜单
                    contextMenuLayer.Show(treeView1, e.Location);
                }
            }
        }

        private void 打开属性表ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 获取当前选中的节点
            TreeNode node = treeView1.SelectedNode;

            // 再次确认它是不是矢量图层
            if (node != null && node.Tag is XVectorLayer layer)
            {
                // 创建并显示属性表窗体 [cite: 822]
                FormAttribute form = new FormAttribute(layer);
                form.Show(); // 使用 Show() 可以同时看地图和表，ShowDialog() 则必须关掉表才能动地图
            }

        }

        private void 缩放至图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node != null && node.Tag is XVectorLayer layer)
            {
                // 判空：万一是个空图层（没有要素），Extent 可能是 null
                if (layer.Extent == null)
                {
                    MessageBox.Show("该图层为空，无法缩放。");
                    return;
                }

                // 核心逻辑：更新视图范围
                view.Update(layer.Extent, mapBox.ClientRectangle);

                // 刷新地图
                UpdateMap();

            }
        }
        private void 移除图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node == null) return;

            // 1. 弹出确认框 (用户体验优化)
            if (MessageBox.Show($"确定要移除图层 \"{node.Text}\" 吗？", "移除确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // 2. 从 TreeView 中移除节点
                // 因为我们的绘制逻辑(UpdateMap)是遍历 TreeView.Nodes 的，
                // 所以只要从树上删了，地图上也就自动没了。
                treeView1.Nodes.Remove(node);

                // 3. 刷新地图
                UpdateMap();

                // 4. 更新状态栏显示的选中数量 (因为移除了可能包含选中要素的图层)
                UpdateSelectionStatus();
            }

        }

        private void 注记ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node != null && node.Tag is XVectorLayer layer)
            {
                // 1. 切换状态 (True 变 False，False 变 True)
                layer.LabelOrNot = !layer.LabelOrNot;

                // 2. (可选) 如果你想更智能一点，可以默认让它显示第1列或者第2列的属性
                // layer.LabelIndex = 1; // 默认显示第0列，通常是ID，可能不好看

                // 3. 刷新地图
                UpdateMap();
            }

        }

        private void treeView1_MouseMove(object sender, MouseEventArgs e)
        {
            // 1. 必须是左键按住的状态
            if (e.Button != MouseButtons.Left) return;

            // 2. 必须有一个当前选中的节点
            if (treeView1.SelectedNode == null) return;

            // 3. 【关键】防抖动检测
            // 只有当鼠标移动超过一定距离（比如系统设定的拖拽阈值）时，才算真正的拖动。
            // 这样可以避免点击的时候手抖误触发拖动。
            Size dragSize = SystemInformation.DragSize;
            Rectangle dragRect = new Rectangle(
                new Point(TreeMouseDownLocation.X - dragSize.Width / 2,
                          TreeMouseDownLocation.Y - dragSize.Height / 2),
                dragSize);

            // 如果鼠标移出了那个小小的“静止区域”，说明用户想拖动了
            if (!dragRect.Contains(e.Location))
            {
                // 手动发起拖动！
                // 这行代码和你原本 ItemDrag 里的代码是一样的效果
                DoDragDrop(treeView1.SelectedNode, DragDropEffects.Move);
            }
        }

    }
}