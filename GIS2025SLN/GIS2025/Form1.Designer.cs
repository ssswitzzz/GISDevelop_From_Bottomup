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
            this.bRefresh = new System.Windows.Forms.Button();
            this.labelXY = new System.Windows.Forms.Label();
            this.bZoomIn = new System.Windows.Forms.Button();
            this.bZoomOut = new System.Windows.Forms.Button();
            this.bMoveDown = new System.Windows.Forms.Button();
            this.bMoveUp = new System.Windows.Forms.Button();
            this.bMoveLeft = new System.Windows.Forms.Button();
            this.bMoveRight = new System.Windows.Forms.Button();
            this.bFullExtent = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.bReadShapefile = new System.Windows.Forms.Button();
            this.bOpenAttribute = new System.Windows.Forms.Button();
            this.bReadMyFile = new System.Windows.Forms.Button();
            this.bWriteMyFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // bRefresh
            // 
            this.bRefresh.Location = new System.Drawing.Point(151, 16);
            this.bRefresh.Name = "bRefresh";
            this.bRefresh.Size = new System.Drawing.Size(120, 29);
            this.bRefresh.TabIndex = 4;
            this.bRefresh.Text = "重绘";
            this.bRefresh.UseVisualStyleBackColor = true;
            this.bRefresh.Click += new System.EventHandler(this.bRefresh_Click);
            // 
            // labelXY
            // 
            this.labelXY.AutoSize = true;
            this.labelXY.Font = new System.Drawing.Font("宋体", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelXY.Location = new System.Drawing.Point(277, 20);
            this.labelXY.Name = "labelXY";
            this.labelXY.Size = new System.Drawing.Size(82, 21);
            this.labelXY.TabIndex = 7;
            this.labelXY.Text = "label5";
            // 
            // bZoomIn
            // 
            this.bZoomIn.Location = new System.Drawing.Point(477, 16);
            this.bZoomIn.Name = "bZoomIn";
            this.bZoomIn.Size = new System.Drawing.Size(53, 29);
            this.bZoomIn.TabIndex = 3;
            this.bZoomIn.Text = "放大";
            this.bZoomIn.UseVisualStyleBackColor = true;
            this.bZoomIn.Click += new System.EventHandler(this.ExploreButton_Click);
            // 
            // bZoomOut
            // 
            this.bZoomOut.Location = new System.Drawing.Point(536, 16);
            this.bZoomOut.Name = "bZoomOut";
            this.bZoomOut.Size = new System.Drawing.Size(53, 29);
            this.bZoomOut.TabIndex = 3;
            this.bZoomOut.Text = "缩小";
            this.bZoomOut.UseVisualStyleBackColor = true;
            this.bZoomOut.Click += new System.EventHandler(this.ExploreButton_Click);
            // 
            // bMoveDown
            // 
            this.bMoveDown.Location = new System.Drawing.Point(654, 16);
            this.bMoveDown.Name = "bMoveDown";
            this.bMoveDown.Size = new System.Drawing.Size(53, 29);
            this.bMoveDown.TabIndex = 3;
            this.bMoveDown.Text = "向下";
            this.bMoveDown.UseVisualStyleBackColor = true;
            this.bMoveDown.Click += new System.EventHandler(this.ExploreButton_Click);
            // 
            // bMoveUp
            // 
            this.bMoveUp.Location = new System.Drawing.Point(595, 16);
            this.bMoveUp.Name = "bMoveUp";
            this.bMoveUp.Size = new System.Drawing.Size(53, 29);
            this.bMoveUp.TabIndex = 3;
            this.bMoveUp.Text = "向上";
            this.bMoveUp.UseVisualStyleBackColor = true;
            this.bMoveUp.Click += new System.EventHandler(this.ExploreButton_Click);
            // 
            // bMoveLeft
            // 
            this.bMoveLeft.Location = new System.Drawing.Point(713, 16);
            this.bMoveLeft.Name = "bMoveLeft";
            this.bMoveLeft.Size = new System.Drawing.Size(53, 29);
            this.bMoveLeft.TabIndex = 3;
            this.bMoveLeft.Text = "向左";
            this.bMoveLeft.UseVisualStyleBackColor = true;
            this.bMoveLeft.Click += new System.EventHandler(this.ExploreButton_Click);
            // 
            // bMoveRight
            // 
            this.bMoveRight.Location = new System.Drawing.Point(772, 16);
            this.bMoveRight.Name = "bMoveRight";
            this.bMoveRight.Size = new System.Drawing.Size(53, 29);
            this.bMoveRight.TabIndex = 3;
            this.bMoveRight.Text = "向右";
            this.bMoveRight.UseVisualStyleBackColor = true;
            this.bMoveRight.Click += new System.EventHandler(this.ExploreButton_Click);
            // 
            // bFullExtent
            // 
            this.bFullExtent.Location = new System.Drawing.Point(860, 16);
            this.bFullExtent.Name = "bFullExtent";
            this.bFullExtent.Size = new System.Drawing.Size(53, 29);
            this.bFullExtent.TabIndex = 3;
            this.bFullExtent.Text = "全图";
            this.bFullExtent.UseVisualStyleBackColor = true;
            this.bFullExtent.Click += new System.EventHandler(this.bFullExtent_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.checkBox1.Location = new System.Drawing.Point(26, 62);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(118, 24);
            this.checkBox1.TabIndex = 8;
            this.checkBox1.Text = "checkBox1";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // bReadShapefile
            // 
            this.bReadShapefile.Location = new System.Drawing.Point(151, 57);
            this.bReadShapefile.Name = "bReadShapefile";
            this.bReadShapefile.Size = new System.Drawing.Size(120, 29);
            this.bReadShapefile.TabIndex = 4;
            this.bReadShapefile.Text = "读Shapefile";
            this.bReadShapefile.UseVisualStyleBackColor = true;
            this.bReadShapefile.Click += new System.EventHandler(this.bReadShapefile_Click);
            // 
            // bOpenAttribute
            // 
            this.bOpenAttribute.Location = new System.Drawing.Point(294, 57);
            this.bOpenAttribute.Name = "bOpenAttribute";
            this.bOpenAttribute.Size = new System.Drawing.Size(120, 29);
            this.bOpenAttribute.TabIndex = 4;
            this.bOpenAttribute.Text = "打开属性窗口";
            this.bOpenAttribute.UseVisualStyleBackColor = true;
            this.bOpenAttribute.Click += new System.EventHandler(this.bOpenAttribute_Click);
            // 
            // bReadMyFile
            // 
            this.bReadMyFile.Location = new System.Drawing.Point(445, 57);
            this.bReadMyFile.Name = "bReadMyFile";
            this.bReadMyFile.Size = new System.Drawing.Size(120, 29);
            this.bReadMyFile.TabIndex = 4;
            this.bReadMyFile.Text = "读myfile";
            this.bReadMyFile.UseVisualStyleBackColor = true;
            this.bReadMyFile.Click += new System.EventHandler(this.bReadMyFile_Click);
            // 
            // bWriteMyFile
            // 
            this.bWriteMyFile.Location = new System.Drawing.Point(587, 57);
            this.bWriteMyFile.Name = "bWriteMyFile";
            this.bWriteMyFile.Size = new System.Drawing.Size(120, 29);
            this.bWriteMyFile.TabIndex = 4;
            this.bWriteMyFile.Text = "写myfile";
            this.bWriteMyFile.UseVisualStyleBackColor = true;
            this.bWriteMyFile.Click += new System.EventHandler(this.bWriteMyFile_Click);
            // 
            // FormMap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(955, 450);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.labelXY);
            this.Controls.Add(this.bOpenAttribute);
            this.Controls.Add(this.bWriteMyFile);
            this.Controls.Add(this.bReadMyFile);
            this.Controls.Add(this.bReadShapefile);
            this.Controls.Add(this.bRefresh);
            this.Controls.Add(this.bFullExtent);
            this.Controls.Add(this.bMoveRight);
            this.Controls.Add(this.bMoveLeft);
            this.Controls.Add(this.bMoveUp);
            this.Controls.Add(this.bMoveDown);
            this.Controls.Add(this.bZoomOut);
            this.Controls.Add(this.bZoomIn);
            this.Name = "FormMap";
            this.Text = "Form1";
            this.SizeChanged += new System.EventHandler(this.FormMap_SizeChanged);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.FormMap_Paint);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FormMap_MouseClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FormMap_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FormMap_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.FormMap_MouseUp);
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.FormMap_MouseWheel);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button bRefresh;
        private System.Windows.Forms.Label labelXY;
        private System.Windows.Forms.Button bZoomIn;
        private System.Windows.Forms.Button bZoomOut;
        private System.Windows.Forms.Button bMoveDown;
        private System.Windows.Forms.Button bMoveUp;
        private System.Windows.Forms.Button bMoveLeft;
        private System.Windows.Forms.Button bMoveRight;
        private System.Windows.Forms.Button bFullExtent;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Button bReadShapefile;
        private System.Windows.Forms.Button bOpenAttribute;
        private System.Windows.Forms.Button bReadMyFile;
        private System.Windows.Forms.Button bWriteMyFile;
    }
}

