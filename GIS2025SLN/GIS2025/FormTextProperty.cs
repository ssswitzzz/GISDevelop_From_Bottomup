using System;
using System.Drawing;
using System.Windows.Forms;
using XGIS;

namespace GIS2025
{
    public partial class FormTextProperty : Form
    {
        private XTextElement _element;

        private TextBox txtContent;
        private Button btnFont;
        private Panel pnlColor;
        private CheckBox chkOutline;
        private Panel pnlOutlineColor;
        private NumericUpDown numOutlineWidth;
        private Label lblPreview;
        private Panel pnlPreviewContainer;

        private Font _tempFont;
        private Color _tempColor;
        private Color _tempOutlineColor;

        public FormTextProperty(XTextElement element)
        {
            _element = element;
            _tempFont = (Font)element.Font.Clone();
            _tempColor = element.Color;
            _tempOutlineColor = element.OutlineColor;

            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Text = "文本属性设置";
            this.Size = new Size(480, 550);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int padding = 20;
            int labelWidth = 80;
            int inputX = padding + labelWidth + 10;
            int inputWidth = 300;
            int y = 20;


            Label lblContent = new Label() { Text = "文本内容:", Location = new Point(padding, y + 3), AutoSize = true };
            txtContent = new TextBox() { Location = new Point(inputX, y), Width = inputWidth };
            txtContent.TextChanged += (s, e) => UpdatePreview();
            this.Controls.Add(lblContent);
            this.Controls.Add(txtContent);
            y += 45;

            Label lblFont = new Label() { Text = "字体样式:", Location = new Point(padding, y + 3), AutoSize = true };
            btnFont = new Button() { Text = "点击修改字体...", Location = new Point(inputX, y - 2), Width = inputWidth, Height = 28 };
            btnFont.Click += BtnFont_Click;
            this.Controls.Add(lblFont);
            this.Controls.Add(btnFont);
            y += 45;

            Label lblColor = new Label() { Text = "文字颜色:", Location = new Point(padding, y + 3), AutoSize = true };
            pnlColor = new Panel() { Location = new Point(inputX, y), Size = new Size(60, 24), BorderStyle = BorderStyle.FixedSingle, BackColor = _tempColor, Cursor = Cursors.Hand };
            pnlColor.Click += (s, e) => PickColor(ref _tempColor, pnlColor);
            this.Controls.Add(lblColor);
            this.Controls.Add(pnlColor);
            y += 45;

            GroupBox grpOutline = new GroupBox() { Text = "描边设置", Location = new Point(padding, y), Size = new Size(410, 100) };
            this.Controls.Add(grpOutline);

            chkOutline = new CheckBox() { Text = "启用描边效果", Location = new Point(20, 25), AutoSize = true };
            chkOutline.CheckedChanged += (s, e) => {
                pnlOutlineColor.Enabled = chkOutline.Checked;
                numOutlineWidth.Enabled = chkOutline.Checked;
                UpdatePreview();
            };
            grpOutline.Controls.Add(chkOutline);

            int row2Y = 60;

            Label lblOutColor = new Label() { Text = "颜色:", Location = new Point(20, row2Y + 3), AutoSize = true };
            pnlOutlineColor = new Panel() { Location = new Point(80, row2Y), Size = new Size(50, 22), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            pnlOutlineColor.Click += (s, e) => { if (chkOutline.Checked) PickColor(ref _tempOutlineColor, pnlOutlineColor); };

            Label lblWidth = new Label() { Text = "宽度:", Location = new Point(170, row2Y + 3), AutoSize = true };
            numOutlineWidth = new NumericUpDown() { Location = new Point(220, row2Y), Width = 60, Minimum = 0, Maximum = 50, DecimalPlaces = 1, Increment = 0.5M };
            numOutlineWidth.ValueChanged += (s, e) => UpdatePreview();

            Label lblUnit = new Label() { Text = "pt", Location = new Point(285, row2Y + 3), AutoSize = true };

            grpOutline.Controls.Add(lblOutColor);
            grpOutline.Controls.Add(pnlOutlineColor);
            grpOutline.Controls.Add(lblWidth);
            grpOutline.Controls.Add(numOutlineWidth);
            grpOutline.Controls.Add(lblUnit);

            y += 120;
            Label lblPreTitle = new Label() { Text = "预览:", Location = new Point(padding, y), AutoSize = true };
            this.Controls.Add(lblPreTitle);
            y += 25;

            pnlPreviewContainer = new Panel()
            {
                Location = new Point(padding, y),
                Size = new Size(410, 120),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                BackColor = Color.White
            };

            lblPreview = new Label() { Location = new Point(5, 5), AutoSize = true, Text = "Preview" };
            pnlPreviewContainer.Controls.Add(lblPreview);
            this.Controls.Add(pnlPreviewContainer);

            // 6. 底部按钮
            int btnY = this.ClientSize.Height - 50;
            Button btnOk = new Button() { Text = "确定", DialogResult = DialogResult.OK, Location = new Point(this.ClientSize.Width - 200, btnY), Width = 80, Height = 30 };
            Button btnCancel = new Button() { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(this.ClientSize.Width - 100, btnY), Width = 80, Height = 30 };

            btnOk.Click += BtnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void LoadData()
        {
            txtContent.Text = _element.Text;
            btnFont.Text = $"{_element.Font.Name}, {_element.Font.Size}pt";
            pnlColor.BackColor = _element.Color;
            chkOutline.Checked = _element.UseOutline;
            pnlOutlineColor.BackColor = _element.OutlineColor;
            pnlOutlineColor.Enabled = _element.UseOutline;
            numOutlineWidth.Enabled = _element.UseOutline;
            numOutlineWidth.Value = (decimal)_element.OutlineWidth;
            UpdatePreview();
        }

        private void BtnFont_Click(object sender, EventArgs e)
        {
            FontDialog fd = new FontDialog();

            fd.ShowColor = false;
            fd.ShowEffects = false;
            fd.AllowVerticalFonts = false;

            fd.Font = _tempFont ?? new Font("Arial", 12);

            if (fd.ShowDialog() == DialogResult.OK)
            {
                _tempFont = fd.Font;
                btnFont.Text = $"{_tempFont.Name}, {_tempFont.Size}pt";
                UpdatePreview();
            }
        }

        private void PickColor(ref Color targetColor, Panel pnl)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = targetColor;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                targetColor = cd.Color;
                pnl.BackColor = targetColor;
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            if (lblPreview == null || pnlPreviewContainer == null) return;

            pnlPreviewContainer.SuspendLayout();
            lblPreview.SuspendLayout();

            lblPreview.Text = txtContent.Text;


            lblPreview.Font = _tempFont;
            lblPreview.ForeColor = _tempColor;
            lblPreview.ResumeLayout();
            pnlPreviewContainer.ResumeLayout();
            pnlPreviewContainer.PerformLayout();

            lblPreview.Invalidate();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {

            _element.Text = txtContent.Text;
            _element.Font = _tempFont;
            _element.Color = _tempColor;
            _element.UseOutline = chkOutline.Checked;
            _element.OutlineColor = _tempOutlineColor;
            _element.OutlineWidth = (float)numOutlineWidth.Value;
        }
    }
}