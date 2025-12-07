using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace GIS2025
{
    public partial class FormExport : Form
    {
        public int ExportDPI { get; private set; }
        public ImageFormat ExportFormat { get; private set; }
        public long ExportQuality { get; private set; } // 0-100

        private ComboBox cboFormat;
        private NumericUpDown numDPI;
        private TrackBar trackQuality;
        private Label lblQualityVal;
        private Button btnExport;

        public FormExport()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "导出布局设置";
            // 1. 【改动】加大窗体尺寸，宽一点高一点
            this.Size = new Size(500, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // 定义布局常量，方便调整
            int leftLabelX = 40;   // 左侧标签的 X 坐标
            int leftControlX = 140; // 控件（下拉框等）的起始 X 坐标
            int startY = 40;       // 起始 Y 坐标
            int stepY = 70;        // 【改动】每行间隔加大到 70，防止压盖

            int currentY = startY;

            // ==========================================
            // 第一行：导出格式
            // ==========================================
            Label lblFmt = new Label() { Text = "导出格式:", Location = new Point(leftLabelX, currentY + 3), AutoSize = true, Font = new Font("微软雅黑", 10) };
            this.Controls.Add(lblFmt);

            cboFormat = new ComboBox() { Location = new Point(leftControlX, currentY), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("微软雅黑", 9) };
            cboFormat.Items.AddRange(new string[] { "PNG", "JPEG", "BMP", "TIFF" });
            cboFormat.SelectedIndex = 1; // 默认 JPEG
            cboFormat.SelectedIndexChanged += (s, e) => {
                // 只有 JPEG 模式下才启用质量滑块
                trackQuality.Enabled = cboFormat.SelectedIndex == 1;
                if (!trackQuality.Enabled) lblQualityVal.ForeColor = Color.Gray;
                else lblQualityVal.ForeColor = Color.Black;
            };
            this.Controls.Add(cboFormat);

            currentY += stepY;

            // ==========================================
            // 第二行：分辨率 (DPI)
            // ==========================================
            Label lblDPI = new Label() { Text = "分辨率 (DPI):", Location = new Point(leftLabelX, currentY + 3), AutoSize = true, Font = new Font("微软雅黑", 10) };
            this.Controls.Add(lblDPI);

            numDPI = new NumericUpDown() { Location = new Point(leftControlX+30, currentY), Width = 100, Minimum = 72, Maximum = 1200, Value = 300, Font = new Font("微软雅黑", 9) };
            this.Controls.Add(numDPI);

            

            currentY += stepY;

            // ==========================================
            // 第三行：压缩质量
            // ==========================================
            Label lblQuality = new Label() { Text = "图像质量:", Location = new Point(leftLabelX, currentY + 3), AutoSize = true, Font = new Font("微软雅黑", 10) };
            this.Controls.Add(lblQuality);

            trackQuality = new TrackBar() { Location = new Point(leftControlX, currentY), Width = 220, Minimum = 10, Maximum = 100, Value = 90, TickFrequency = 10 };
            this.Controls.Add(trackQuality);

            // 显示数值的标签放在滑块右边
            lblQualityVal = new Label() { Text = "90", Location = new Point(leftControlX + 230, currentY + 3), AutoSize = true, Font = new Font("微软雅黑", 10, FontStyle.Bold) };
            trackQuality.Scroll += (s, e) => { lblQualityVal.Text = trackQuality.Value.ToString(); };
            this.Controls.Add(lblQualityVal);

            currentY += 70; // 按钮离得远一点

            // ==========================================
            // 底部按钮
            // ==========================================
            btnExport = new Button()
            {
                Text = "确认导出",
                // 按钮居中计算： (窗体宽度 - 按钮宽度) / 2
                Location = new Point((this.ClientSize.Width - 140) / 2, currentY),
                Width = 140,
                Height = 40,
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 10),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExport.Click += BtnExport_Click;
            this.Controls.Add(btnExport);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            ExportDPI = (int)numDPI.Value;
            ExportQuality = trackQuality.Value;

            switch (cboFormat.SelectedIndex)
            {
                case 0: ExportFormat = ImageFormat.Png; break;
                case 1: ExportFormat = ImageFormat.Jpeg; break;
                case 2: ExportFormat = ImageFormat.Bmp; break;
                case 3: ExportFormat = ImageFormat.Tiff; break;
                default: ExportFormat = ImageFormat.Png; break;
            }
        }
    }
}