namespace GIS2025
{
    partial class LayoutControl
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

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.layoutBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.layoutBox)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutBox
            // 
            this.layoutBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutBox.Location = new System.Drawing.Point(0, 0);
            this.layoutBox.Name = "layoutBox";
            this.layoutBox.Size = new System.Drawing.Size(150, 150);
            this.layoutBox.TabIndex = 0;
            this.layoutBox.TabStop = false;
            // 
            // LayoutControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.layoutBox);
            this.Name = "LayoutControl";
            ((System.ComponentModel.ISupportInitialize)(this.layoutBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox layoutBox;
    }
}
