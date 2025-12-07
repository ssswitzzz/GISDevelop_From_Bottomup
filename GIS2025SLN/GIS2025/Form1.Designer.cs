namespace GIS2025
{
    partial class FormMap
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblCoordinates = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblSelectCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.explore_button = new System.Windows.Forms.Button();
            this.button_ReadShp = new System.Windows.Forms.Button();
            this.button_FullExtent = new System.Windows.Forms.Button();
            this.btnSelect = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnAddText = new System.Windows.Forms.Button();
            this.btnAddGrid = new System.Windows.Forms.Button();
            this.btnAddScaleBar = new System.Windows.Forms.Button();
            this.btnAddNorthArrow = new System.Windows.Forms.Button();
            this.btnAddMapFrame = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.mapBox = new System.Windows.Forms.PictureBox();
            this.myLayoutControl = new GIS2025.LayoutControl();
            this.contextMenuLayer = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.打开属性表ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.缩放至图层ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.注记ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.注记属性ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.移除图层ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsNorthArrow = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.simpleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.circleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.starToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsScaleBar = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiScaleLine = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiScaleBar = new System.Windows.Forms.ToolStripMenuItem();
            this.符号系统ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mapBox)).BeginInit();
            this.contextMenuLayer.SuspendLayout();
            this.cmsNorthArrow.SuspendLayout();
            this.cmsScaleBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblCoordinates,
            this.lblSelectCount});
            this.statusStrip1.Location = new System.Drawing.Point(0, 987);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1706, 31);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblCoordinates
            // 
            this.lblCoordinates.Name = "lblCoordinates";
            this.lblCoordinates.Size = new System.Drawing.Size(114, 24);
            this.lblCoordinates.Text = "Coordinates";
            // 
            // lblSelectCount
            // 
            this.lblSelectCount.Name = "lblSelectCount";
            this.lblSelectCount.Size = new System.Drawing.Size(75, 24);
            this.lblSelectCount.Text = "选中：0";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControl1.Font = new System.Drawing.Font("思源宋体 CN SemiBold", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1706, 120);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.flowLayoutPanel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 38);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1698, 78);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "地图操作";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.explore_button);
            this.flowLayoutPanel1.Controls.Add(this.button_ReadShp);
            this.flowLayoutPanel1.Controls.Add(this.button_FullExtent);
            this.flowLayoutPanel1.Controls.Add(this.btnSelect);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1692, 72);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // explore_button
            // 
            this.explore_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.explore_button.Location = new System.Drawing.Point(3, 3);
            this.explore_button.Name = "explore_button";
            this.explore_button.Size = new System.Drawing.Size(113, 69);
            this.explore_button.TabIndex = 0;
            this.explore_button.Text = "Explore";
            this.explore_button.UseVisualStyleBackColor = true;
            this.explore_button.Click += new System.EventHandler(this.explore_button_Click);
            // 
            // button_ReadShp
            // 
            this.button_ReadShp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_ReadShp.Location = new System.Drawing.Point(122, 3);
            this.button_ReadShp.Name = "button_ReadShp";
            this.button_ReadShp.Size = new System.Drawing.Size(135, 69);
            this.button_ReadShp.TabIndex = 1;
            this.button_ReadShp.Text = "读取Shapefile";
            this.button_ReadShp.UseVisualStyleBackColor = true;
            this.button_ReadShp.Click += new System.EventHandler(this.button_ReadShp_Click);
            // 
            // button_FullExtent
            // 
            this.button_FullExtent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_FullExtent.Location = new System.Drawing.Point(263, 3);
            this.button_FullExtent.Name = "button_FullExtent";
            this.button_FullExtent.Size = new System.Drawing.Size(113, 69);
            this.button_FullExtent.TabIndex = 2;
            this.button_FullExtent.Text = "全图";
            this.button_FullExtent.UseVisualStyleBackColor = true;
            this.button_FullExtent.Click += new System.EventHandler(this.button_FullExtent_Click);
            // 
            // btnSelect
            // 
            this.btnSelect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelect.Location = new System.Drawing.Point(382, 3);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(113, 69);
            this.btnSelect.TabIndex = 3;
            this.btnSelect.Text = "选择";
            this.btnSelect.UseVisualStyleBackColor = true;
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btnAddText);
            this.tabPage2.Controls.Add(this.btnAddGrid);
            this.tabPage2.Controls.Add(this.btnAddScaleBar);
            this.tabPage2.Controls.Add(this.btnAddNorthArrow);
            this.tabPage2.Controls.Add(this.btnAddMapFrame);
            this.tabPage2.Location = new System.Drawing.Point(4, 38);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1698, 78);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Layout";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnAddText
            // 
            this.btnAddText.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAddText.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddText.Location = new System.Drawing.Point(835, 3);
            this.btnAddText.Name = "btnAddText";
            this.btnAddText.Size = new System.Drawing.Size(208, 72);
            this.btnAddText.TabIndex = 5;
            this.btnAddText.Text = "添加文本框";
            this.btnAddText.UseVisualStyleBackColor = true;
            this.btnAddText.Click += new System.EventHandler(this.btnAddText_Click);
            // 
            // btnAddGrid
            // 
            this.btnAddGrid.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAddGrid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddGrid.Location = new System.Drawing.Point(627, 3);
            this.btnAddGrid.Name = "btnAddGrid";
            this.btnAddGrid.Size = new System.Drawing.Size(208, 72);
            this.btnAddGrid.TabIndex = 4;
            this.btnAddGrid.Text = "添加经纬网";
            this.btnAddGrid.UseVisualStyleBackColor = true;
            this.btnAddGrid.Click += new System.EventHandler(this.btnAddGrid_Click);
            // 
            // btnAddScaleBar
            // 
            this.btnAddScaleBar.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAddScaleBar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddScaleBar.Location = new System.Drawing.Point(419, 3);
            this.btnAddScaleBar.Name = "btnAddScaleBar";
            this.btnAddScaleBar.Size = new System.Drawing.Size(208, 72);
            this.btnAddScaleBar.TabIndex = 3;
            this.btnAddScaleBar.Text = "添加比例尺";
            this.btnAddScaleBar.UseVisualStyleBackColor = true;
            this.btnAddScaleBar.Click += new System.EventHandler(this.btnAddScaleBar_Click);
            // 
            // btnAddNorthArrow
            // 
            this.btnAddNorthArrow.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAddNorthArrow.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddNorthArrow.Location = new System.Drawing.Point(211, 3);
            this.btnAddNorthArrow.Name = "btnAddNorthArrow";
            this.btnAddNorthArrow.Size = new System.Drawing.Size(208, 72);
            this.btnAddNorthArrow.TabIndex = 2;
            this.btnAddNorthArrow.Text = "添加指北针";
            this.btnAddNorthArrow.UseVisualStyleBackColor = true;
            this.btnAddNorthArrow.Click += new System.EventHandler(this.btnAddNorthArrow_Click);
            // 
            // btnAddMapFrame
            // 
            this.btnAddMapFrame.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAddMapFrame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddMapFrame.Location = new System.Drawing.Point(3, 3);
            this.btnAddMapFrame.Name = "btnAddMapFrame";
            this.btnAddMapFrame.Size = new System.Drawing.Size(208, 72);
            this.btnAddMapFrame.TabIndex = 1;
            this.btnAddMapFrame.Text = "添加Map Frame";
            this.btnAddMapFrame.UseVisualStyleBackColor = true;
            this.btnAddMapFrame.Click += new System.EventHandler(this.btnAddMapFrame_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 120);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeView1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.mapBox);
            this.splitContainer1.Panel2.Controls.Add(this.myLayoutControl);
            this.splitContainer1.Size = new System.Drawing.Size(1706, 867);
            this.splitContainer1.SplitterDistance = 282;
            this.splitContainer1.TabIndex = 2;
            // 
            // treeView1
            // 
            this.treeView1.AllowDrop = true;
            this.treeView1.BackColor = System.Drawing.SystemColors.Menu;
            this.treeView1.CheckBoxes = true;
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.HideSelection = false;
            this.treeView1.Location = new System.Drawing.Point(0, 0);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(282, 867);
            this.treeView1.TabIndex = 0;
            this.treeView1.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterCheck);
            // 
            // mapBox
            // 
            this.mapBox.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.mapBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapBox.Location = new System.Drawing.Point(0, 0);
            this.mapBox.Name = "mapBox";
            this.mapBox.Size = new System.Drawing.Size(1420, 867);
            this.mapBox.TabIndex = 0;
            this.mapBox.TabStop = false;
            this.mapBox.SizeChanged += new System.EventHandler(this.mapBox_SizeChanged);
            this.mapBox.Paint += new System.Windows.Forms.PaintEventHandler(this.mapBox_Paint);
            this.mapBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.mapBox_MouseDown);
            this.mapBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.mapBox_MouseMove);
            this.mapBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.mapBox_MouseUp);
            // 
            // myLayoutControl
            // 
            this.myLayoutControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myLayoutControl.Location = new System.Drawing.Point(0, 0);
            this.myLayoutControl.Name = "myLayoutControl";
            this.myLayoutControl.Size = new System.Drawing.Size(1420, 867);
            this.myLayoutControl.TabIndex = 1;
            // 
            // contextMenuLayer
            // 
            this.contextMenuLayer.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuLayer.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.打开属性表ToolStripMenuItem,
            this.缩放至图层ToolStripMenuItem,
            this.符号系统ToolStripMenuItem,
            this.注记ToolStripMenuItem,
            this.注记属性ToolStripMenuItem,
            this.移除图层ToolStripMenuItem});
            this.contextMenuLayer.Name = "contextMenuLayer";
            this.contextMenuLayer.Size = new System.Drawing.Size(241, 217);
            // 
            // 打开属性表ToolStripMenuItem
            // 
            this.打开属性表ToolStripMenuItem.Name = "打开属性表ToolStripMenuItem";
            this.打开属性表ToolStripMenuItem.Size = new System.Drawing.Size(240, 30);
            this.打开属性表ToolStripMenuItem.Text = "打开属性表";
            this.打开属性表ToolStripMenuItem.Click += new System.EventHandler(this.打开属性表ToolStripMenuItem_Click);
            // 
            // 缩放至图层ToolStripMenuItem
            // 
            this.缩放至图层ToolStripMenuItem.Name = "缩放至图层ToolStripMenuItem";
            this.缩放至图层ToolStripMenuItem.Size = new System.Drawing.Size(240, 30);
            this.缩放至图层ToolStripMenuItem.Text = "缩放至图层";
            this.缩放至图层ToolStripMenuItem.Click += new System.EventHandler(this.缩放至图层ToolStripMenuItem_Click);
            // 
            // 注记ToolStripMenuItem
            // 
            this.注记ToolStripMenuItem.Name = "注记ToolStripMenuItem";
            this.注记ToolStripMenuItem.Size = new System.Drawing.Size(240, 30);
            this.注记ToolStripMenuItem.Text = "注记";
            this.注记ToolStripMenuItem.Click += new System.EventHandler(this.注记ToolStripMenuItem_Click);
            // 
            // 注记属性ToolStripMenuItem
            // 
            this.注记属性ToolStripMenuItem.Name = "注记属性ToolStripMenuItem";
            this.注记属性ToolStripMenuItem.Size = new System.Drawing.Size(240, 30);
            this.注记属性ToolStripMenuItem.Text = "注记属性";
            this.注记属性ToolStripMenuItem.Click += new System.EventHandler(this.注记属性ToolStripMenuItem_Click);
            // 
            // 移除图层ToolStripMenuItem
            // 
            this.移除图层ToolStripMenuItem.Name = "移除图层ToolStripMenuItem";
            this.移除图层ToolStripMenuItem.Size = new System.Drawing.Size(240, 30);
            this.移除图层ToolStripMenuItem.Text = "移除图层";
            this.移除图层ToolStripMenuItem.Click += new System.EventHandler(this.移除图层ToolStripMenuItem_Click);
            // 
            // cmsNorthArrow
            // 
            this.cmsNorthArrow.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.cmsNorthArrow.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.simpleToolStripMenuItem,
            this.circleToolStripMenuItem,
            this.starToolStripMenuItem});
            this.cmsNorthArrow.Name = "cmsNorthArrow";
            this.cmsNorthArrow.Size = new System.Drawing.Size(140, 94);
            // 
            // simpleToolStripMenuItem
            // 
            this.simpleToolStripMenuItem.Name = "simpleToolStripMenuItem";
            this.simpleToolStripMenuItem.Size = new System.Drawing.Size(139, 30);
            this.simpleToolStripMenuItem.Text = "Simple";
            this.simpleToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItemSimple_Click);
            // 
            // circleToolStripMenuItem
            // 
            this.circleToolStripMenuItem.Name = "circleToolStripMenuItem";
            this.circleToolStripMenuItem.Size = new System.Drawing.Size(139, 30);
            this.circleToolStripMenuItem.Text = "Circle";
            this.circleToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItemCircle_Click);
            // 
            // starToolStripMenuItem
            // 
            this.starToolStripMenuItem.Name = "starToolStripMenuItem";
            this.starToolStripMenuItem.Size = new System.Drawing.Size(139, 30);
            this.starToolStripMenuItem.Text = "Star";
            this.starToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItemStar_Click);
            // 
            // cmsScaleBar
            // 
            this.cmsScaleBar.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.cmsScaleBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiScaleLine,
            this.tsmiScaleBar});
            this.cmsScaleBar.Name = "cmsScaleBar";
            this.cmsScaleBar.Size = new System.Drawing.Size(116, 64);
            // 
            // tsmiScaleLine
            // 
            this.tsmiScaleLine.Name = "tsmiScaleLine";
            this.tsmiScaleLine.Size = new System.Drawing.Size(115, 30);
            this.tsmiScaleLine.Text = "Line";
            this.tsmiScaleLine.Click += new System.EventHandler(this.tsmiScaleLine_Click);
            // 
            // tsmiScaleBar
            // 
            this.tsmiScaleBar.Name = "tsmiScaleBar";
            this.tsmiScaleBar.Size = new System.Drawing.Size(115, 30);
            this.tsmiScaleBar.Text = "Bar";
            this.tsmiScaleBar.Click += new System.EventHandler(this.tsmiScaleBar_Click);
            // 
            // 符号系统ToolStripMenuItem
            // 
            this.符号系统ToolStripMenuItem.Name = "符号系统ToolStripMenuItem";
            this.符号系统ToolStripMenuItem.Size = new System.Drawing.Size(240, 30);
            this.符号系统ToolStripMenuItem.Text = "符号系统";
            this.符号系统ToolStripMenuItem.Click += new System.EventHandler(this.符号系统ToolStripMenuItem_Click);
            // 
            // FormMap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1706, 1018);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusStrip1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FormMap";
            this.Text = "Form1";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mapBox)).EndInit();
            this.contextMenuLayer.ResumeLayout(false);
            this.cmsNorthArrow.ResumeLayout(false);
            this.cmsScaleBar.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblCoordinates;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button explore_button;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button button_ReadShp;
        private System.Windows.Forms.Button button_FullExtent;
        private System.Windows.Forms.ToolStripStatusLabel lblSelectCount;
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.ContextMenuStrip contextMenuLayer;
        private System.Windows.Forms.ToolStripMenuItem 缩放至图层ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 注记ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 移除图层ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 打开属性表ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 注记属性ToolStripMenuItem;
        private System.Windows.Forms.PictureBox mapBox;
        private LayoutControl myLayoutControl;
        private System.Windows.Forms.Button btnAddNorthArrow;
        private System.Windows.Forms.Button btnAddMapFrame;
        private System.Windows.Forms.ContextMenuStrip cmsNorthArrow;
        private System.Windows.Forms.ToolStripMenuItem simpleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem circleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem starToolStripMenuItem;
        private System.Windows.Forms.Button btnAddGrid;
        private System.Windows.Forms.Button btnAddScaleBar;
        private System.Windows.Forms.ContextMenuStrip cmsScaleBar;
        private System.Windows.Forms.ToolStripMenuItem tsmiScaleLine;
        private System.Windows.Forms.ToolStripMenuItem tsmiScaleBar;
        private System.Windows.Forms.Button btnAddText;
        private System.Windows.Forms.ToolStripMenuItem 符号系统ToolStripMenuItem;
    }
}

