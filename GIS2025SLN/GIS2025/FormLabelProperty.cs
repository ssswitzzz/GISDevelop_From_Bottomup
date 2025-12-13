using System;
using System.Drawing;
using System.Windows.Forms;
using XGIS;

namespace GIS2025
{
    public partial class FormLabelProperty : Form
    {
        private XVectorLayer _layer;
        private Font _tempFont;
        private Color _tempColor;
        private Color _tempOutlineColor;

        public FormLabelProperty(XVectorLayer layer)
        {
            InitializeComponent();
            _layer = layer;

            _tempFont = _layer.LabelThematic.LabelFont;
            _tempColor = _layer.LabelThematic.LabelBrush.Color;
            _tempOutlineColor = _layer.LabelThematic.OutlineColor;

            chkOutline.Checked = _layer.LabelThematic.UseOutline;
            picColor.BackColor = _tempColor;
            picOutlineColor.BackColor = _tempOutlineColor;
            lblFontName.Text = _tempFont.Name + " " + _tempFont.Size;
        }

        private void FormLabelProperty_Load(object sender, EventArgs e)
        {
            // 填充字段列表
            for (int i = 0; i < _layer.Fields.Count; i++)
            {
                cmbFields.Items.Add(_layer.Fields[i].name);
            }
            // 选中当前正在使用的字段
            if (_layer.LabelThematic.LabelIndex < cmbFields.Items.Count)
                cmbFields.SelectedIndex = _layer.LabelThematic.LabelIndex;
        }

        // 修改字体
        private void btnFont_Click(object sender, EventArgs e)
        {
            FontDialog fd = new FontDialog();
            fd.Font = _tempFont;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                _tempFont = fd.Font;
                lblFontName.Text = _tempFont.Name + " " + _tempFont.Size;
            }
        }

        // 修改文字颜色
        private void picColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = _tempColor;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                _tempColor = cd.Color;
                picColor.BackColor = _tempColor;
            }
        }

        // 修改描边颜色
        private void picOutlineColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = _tempOutlineColor;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                _tempOutlineColor = cd.Color;
                picOutlineColor.BackColor = _tempOutlineColor;
            }
        }

        // 确定保存
        private void btnOK_Click(object sender, EventArgs e)
        {
            // 将界面上的值写回 Layer
            _layer.LabelThematic.LabelIndex = cmbFields.SelectedIndex;
            _layer.LabelThematic.LabelFont = _tempFont;
            _layer.LabelThematic.LabelBrush = new SolidBrush(_tempColor);
            _layer.LabelThematic.UseOutline = chkOutline.Checked;
            _layer.LabelThematic.OutlineColor = _tempOutlineColor;
            _layer.LabelOrNot = true;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void lblFontName_Click(object sender, EventArgs e)
        {

        }
    }
}