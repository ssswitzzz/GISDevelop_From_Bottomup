namespace GIS2025
{
    partial class FormLabelProperty
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cmbFields = new System.Windows.Forms.ComboBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.btnFont = new System.Windows.Forms.Button();
            this.chkOutline = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.lblFontName = new System.Windows.Forms.Label();
            this.picOutlineColor = new System.Windows.Forms.PictureBox();
            this.picColor = new System.Windows.Forms.PictureBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.picOutlineColor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picColor)).BeginInit();
            this.SuspendLayout();
            // 
            // cmbFields
            // 
            this.cmbFields.Font = new System.Drawing.Font("思源宋体 CN", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbFields.FormattingEnabled = true;
            this.cmbFields.Location = new System.Drawing.Point(165, 53);
            this.cmbFields.Name = "cmbFields";
            this.cmbFields.Size = new System.Drawing.Size(121, 42);
            this.cmbFields.TabIndex = 0;
            // 
            // textBox1
            // 
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Font = new System.Drawing.Font("思源宋体 CN SemiBold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBox1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.textBox1.Location = new System.Drawing.Point(28, 53);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(131, 35);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "选择字段：";
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // btnFont
            // 
            this.btnFont.Font = new System.Drawing.Font("思源宋体 CN SemiBold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnFont.Location = new System.Drawing.Point(155, 152);
            this.btnFont.Name = "btnFont";
            this.btnFont.Size = new System.Drawing.Size(131, 43);
            this.btnFont.TabIndex = 2;
            this.btnFont.Text = "设置字体";
            this.btnFont.UseVisualStyleBackColor = true;
            this.btnFont.Click += new System.EventHandler(this.btnFont_Click);
            // 
            // chkOutline
            // 
            this.chkOutline.AutoSize = true;
            this.chkOutline.Location = new System.Drawing.Point(531, 217);
            this.chkOutline.Name = "chkOutline";
            this.chkOutline.Size = new System.Drawing.Size(106, 22);
            this.chkOutline.TabIndex = 5;
            this.chkOutline.Text = "启用描边";
            this.chkOutline.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(531, 298);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(135, 43);
            this.btnOK.TabIndex = 7;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // lblFontName
            // 
            this.lblFontName.AutoSize = true;
            this.lblFontName.Font = new System.Drawing.Font("思源宋体 CN SemiBold", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblFontName.Location = new System.Drawing.Point(51, 159);
            this.lblFontName.Name = "lblFontName";
            this.lblFontName.Size = new System.Drawing.Size(77, 30);
            this.lblFontName.TabIndex = 8;
            this.lblFontName.Text = "label1";
            // 
            // picOutlineColor
            // 
            this.picOutlineColor.Location = new System.Drawing.Point(531, 152);
            this.picOutlineColor.Name = "picOutlineColor";
            this.picOutlineColor.Size = new System.Drawing.Size(100, 50);
            this.picOutlineColor.TabIndex = 6;
            this.picOutlineColor.TabStop = false;
            this.picOutlineColor.Click += new System.EventHandler(this.picOutlineColor_Click);
            // 
            // picColor
            // 
            this.picColor.Location = new System.Drawing.Point(531, 60);
            this.picColor.Name = "picColor";
            this.picColor.Size = new System.Drawing.Size(100, 50);
            this.picColor.TabIndex = 4;
            this.picColor.TabStop = false;
            this.picColor.Click += new System.EventHandler(this.picColor_Click);
            // 
            // textBox3
            // 
            this.textBox3.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox3.Font = new System.Drawing.Font("思源宋体 CN SemiBold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBox3.Location = new System.Drawing.Point(368, 60);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(133, 35);
            this.textBox3.TabIndex = 10;
            this.textBox3.Text = "文字颜色：";
            // 
            // textBox4
            // 
            this.textBox4.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox4.Font = new System.Drawing.Font("思源宋体 CN SemiBold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBox4.Location = new System.Drawing.Point(368, 156);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(133, 35);
            this.textBox4.TabIndex = 11;
            this.textBox4.Text = "描边颜色：";
            // 
            // FormLabelProperty
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(687, 368);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.lblFontName);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.picOutlineColor);
            this.Controls.Add(this.chkOutline);
            this.Controls.Add(this.picColor);
            this.Controls.Add(this.btnFont);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.cmbFields);
            this.Name = "FormLabelProperty";
            this.Text = "FormLabelProperty";
            this.Load += new System.EventHandler(this.FormLabelProperty_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picOutlineColor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picColor)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbFields;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.Button btnFont;
        private System.Windows.Forms.PictureBox picColor;
        private System.Windows.Forms.CheckBox chkOutline;
        private System.Windows.Forms.PictureBox picOutlineColor;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label lblFontName;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox4;
    }
}