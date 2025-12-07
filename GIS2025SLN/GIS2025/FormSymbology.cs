using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using XGIS;

namespace GIS2025
{
    public partial class FormSymbology : Form
    {
        private XVectorLayer _layer;

        // UI 控件
        private ComboBox cboMode;
        private ComboBox cboField;
        private Panel pnlContainer; // 放置特定模式控件的容器
        private DataGridView dgvPreview;
        private Button btnOK;

        // Single Symbol 控件
        private Panel pnlSingleColor;
        private NumericUpDown numSingleSize;

        // Unique Value 控件
        private Button btnGenerateUnique;

        // Graduated Symbol 控件
        private NumericUpDown numClassCount;
        private Panel pnlStartColor;
        private Panel pnlEndColor;
        private NumericUpDown numMinSize;
        private NumericUpDown numMaxSize;

        // 为了方便管理，将计算按钮设为成员变量
        private Button _btnCalcGraduated;

        public FormSymbology(XVectorLayer layer)
        {
            _layer = layer;
            InitializeComponent(); // 调用系统生成的
            SetupCustomUI();       // 调用我们手写的布局
            LoadLayerData();
        }

        private void SetupCustomUI()
        {
            this.Text = $"符号系统设置 - {_layer.Name}";
            // 1. 【大气版】加大窗体尺寸
            this.Size = new Size(750, 680); // 稍微再宽一点点，防止右侧拥挤
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int padding = 30; // 页边距
            int y = 30;

            // ==========================================
            // --- 顶部控制栏 (布局调整：加大间距) ---
            // ==========================================

            // 1. 模式选择
            // Label 在 30
            Label lblMode = new Label() { Text = "渲染模式:", Location = new Point(padding, y + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
            // ComboBox 原来是 110，改为 140，留出更多空隙
            cboMode = new ComboBox() { Location = new Point(140, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("微软雅黑", 9) };
            cboMode.Items.AddRange(new object[] { "单一符号", "唯一值", "分级符号" });
            cboMode.SelectedIndexChanged += CboMode_SelectedIndexChanged;
            this.Controls.Add(lblMode);
            this.Controls.Add(cboMode);

            // 2. 字段选择 (放在右侧，拉开距离)
            // 原来在 350，改为 400，避免和左边挤在一起
            Label lblField = new Label() { Text = "分类字段:", Location = new Point(400, y + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
            // 原来在 430，改为 480
            cboField = new ComboBox() { Location = new Point(500, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("微软雅黑", 9) };
            this.Controls.Add(lblField);
            this.Controls.Add(cboField);

            y += 50;

            // 3. 动态容器 (加大高度，留足空间)
            // 容器宽度也随窗体加宽
            pnlContainer = new Panel() { Location = new Point(padding, y), Size = new Size(675, 130), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke };
            this.Controls.Add(pnlContainer);

            y += 150; // 跳过容器高度

            // 4. 预览表格
            Label lblPreviewTitle = new Label() { Text = "符号预览:", Location = new Point(padding, y), AutoSize = true };
            this.Controls.Add(lblPreviewTitle);
            y += 25;

            dgvPreview = new DataGridView()
            {
                Location = new Point(padding, y),
                Size = new Size(675, 300), // 表格随窗体变宽
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing, // 1. 允许自定义高度
                ColumnHeadersHeight = 40, // 2. 这里填你想要的高度（像素），默认大概是20多，改成40就宽敞了
            };

            dgvPreview.Columns.Add("Symbol", "符号颜色");
            dgvPreview.Columns.Add("Value", "数值范围 / 类别");
            dgvPreview.Columns.Add("Label", "图例标签");

            // 调整列宽比例
            dgvPreview.Columns[0].Width = 100;
            dgvPreview.Columns[1].Width = 280; // 中间列宽一点
            dgvPreview.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            this.Controls.Add(dgvPreview);

            // 5. 底部按钮 (放在右下角)
            int btnY = this.ClientSize.Height - 60;
            btnOK = new Button() { Text = "确定应用", DialogResult = DialogResult.OK, Location = new Point(this.ClientSize.Width - 240, btnY), Width = 100, Height = 35, Cursor = Cursors.Hand };
            Button btnCancel = new Button() { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(this.ClientSize.Width - 120, btnY), Width = 100, Height = 35, Cursor = Cursors.Hand };
            btnOK.Click += BtnOK_Click;

            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            // --- 初始化子控件 ---
            InitSingleControls();
            InitUniqueControls();
            InitGraduatedControls();
        }

        private void InitSingleControls()
        {
            // 单一符号：
            // Label 位置不变 (30)
            // Control 位置从 110 -> 140
            Label l1 = new Label() { Text = "符号颜色:", Location = new Point(30, 50), AutoSize = true };
            pnlSingleColor = new Panel() { Location = new Point(140, 45), Size = new Size(80, 25), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Red, Cursor = Cursors.Hand };
            pnlSingleColor.Click += (s, e) => PickColor(pnlSingleColor);

            // 第二列控件
            // Label 从 300 -> 380 (拉大中间空隙)
            // Control 从 380 -> 460
            Label l2 = new Label() { Text = "符号大小:", Location = new Point(380, 50), AutoSize = true };
            numSingleSize = new NumericUpDown() { Location = new Point(480, 48), Minimum = 1, Maximum = 50, Value = 3, Width = 80 };
        }

        private void InitUniqueControls()
        {
            // 唯一值：按钮居中显示
            btnGenerateUnique = new Button() { Text = "生成所有唯一值分类", Location = new Point(220, 45), Width = 220, Height = 40, Cursor = Cursors.Hand };
            btnGenerateUnique.Click += (s, e) => GenerateUniqueValues();
        }

        private void InitGraduatedControls()
        {
            // 分级符号：
            // 左侧对齐 X = 140 (原110)
            // 右侧对齐 X = 460 (原380附近)

            // 第一行：分类级数
            Label l1 = new Label() { Text = "分类级数:", Location = new Point(30, 20), AutoSize = true };
            numClassCount = new NumericUpDown() { Location = new Point(140, 18), Minimum = 2, Maximum = 20, Value = 5, Width = 80 };

            // 第二行：颜色渐变
            Label l2 = new Label() { Text = "颜色色带:", Location = new Point(30, 60), AutoSize = true };
            // 起始颜色
            pnlStartColor = new Panel() { Location = new Point(140, 55), Size = new Size(50, 25), BackColor = Color.LightYellow, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            pnlStartColor.Click += (s, e) => PickColor(pnlStartColor);

            // "至" 往右移
            Label lTo = new Label() { Text = "至", Location = new Point(200, 60), AutoSize = true };

            // 结束颜色
            pnlEndColor = new Panel() { Location = new Point(230, 55), Size = new Size(50, 25), BackColor = Color.DarkRed, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            pnlEndColor.Click += (s, e) => PickColor(pnlEndColor);

            // 第三行：尺寸范围 (右侧区域)
            Label l3 = new Label() { Text = "尺寸范围:", Location = new Point(380, 60), AutoSize = true }; // X=380
            numMinSize = new NumericUpDown() { Location = new Point(460, 58), Minimum = 1, Maximum = 50, Value = 3, Width = 60 }; // X=460

            Label lToSize = new Label() { Text = "-", Location = new Point(530, 60), AutoSize = true };
            numMaxSize = new NumericUpDown() { Location = new Point(550, 58), Minimum = 1, Maximum = 100, Value = 15, Width = 60 };

            // 右侧：计算按钮
            // 放在最右边
            _btnCalcGraduated = new Button() { Text = "计算分级", Location = new Point(530, 18), Width = 100, Height = 30, BackColor = Color.AliceBlue, Cursor = Cursors.Hand };
            _btnCalcGraduated.Click += (s, e) => GenerateGraduated();
        }

        private void LoadLayerData()
        {
            cboField.Items.Clear();
            foreach (var f in _layer.Fields)
            {
                cboField.Items.Add(f.name);
            }
            if (cboField.Items.Count > 0) cboField.SelectedIndex = 0;
            cboMode.SelectedIndex = (int)_layer.ThematicMode;
        }

        private void CboMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlContainer.Controls.Clear();
            dgvPreview.Rows.Clear();
            cboField.Enabled = true;

            int mode = cboMode.SelectedIndex;
            if (mode == 0) // Single
            {
                cboField.Enabled = false;
                // 注意：这里需要重新添加 Label，因为 Panel 清空了
                // 使用与 InitSingleControls 一致的坐标
                pnlContainer.Controls.Add(new Label() { Text = "符号颜色:", Location = new Point(30, 50), AutoSize = true });
                pnlContainer.Controls.Add(pnlSingleColor);

                pnlContainer.Controls.Add(new Label() { Text = "符号大小:", Location = new Point(380, 50), AutoSize = true });
                pnlContainer.Controls.Add(numSingleSize);

                dgvPreview.Rows.Add("", "所有要素", "单一符号");
                UpdateSinglePreview();
            }
            else if (mode == 1) // Unique
            {
                pnlContainer.Controls.Add(btnGenerateUnique);
            }
            else if (mode == 2) // Graduated
            {
                // 使用与 InitGraduatedControls 一致的坐标
                pnlContainer.Controls.Add(new Label() { Text = "分类级数:", Location = new Point(30, 20), AutoSize = true });
                pnlContainer.Controls.Add(numClassCount);

                pnlContainer.Controls.Add(new Label() { Text = "颜色色带:", Location = new Point(30, 60), AutoSize = true });
                pnlContainer.Controls.Add(pnlStartColor);
                pnlContainer.Controls.Add(new Label() { Text = "至", Location = new Point(200, 60), AutoSize = true });
                pnlContainer.Controls.Add(pnlEndColor);

                pnlContainer.Controls.Add(new Label() { Text = "尺寸范围:", Location = new Point(380, 60), AutoSize = true });
                pnlContainer.Controls.Add(numMinSize);
                pnlContainer.Controls.Add(new Label() { Text = "-", Location = new Point(530, 60), AutoSize = true });
                pnlContainer.Controls.Add(numMaxSize);

                pnlContainer.Controls.Add(_btnCalcGraduated);
            }
        }

        private void UpdateSinglePreview()
        {
            if (dgvPreview.Rows.Count > 0)
            {
                dgvPreview.Rows[0].Cells[0].Style.BackColor = pnlSingleColor.BackColor;
                dgvPreview.Rows[0].Cells[0].Style.SelectionBackColor = pnlSingleColor.BackColor;
            }
        }

        private void GenerateUniqueValues()
        {
            if (cboField.SelectedIndex == -1) return;
            string fieldName = cboField.SelectedItem.ToString();
            int fieldIndex = _layer.GetFieldIndex(fieldName);

            HashSet<string> uniqueValues = new HashSet<string>();
            foreach (var f in _layer.Features)
            {
                uniqueValues.Add(f.getAttribute(fieldIndex).ToString());
            }

            dgvPreview.Rows.Clear();
            foreach (string val in uniqueValues)
            {
                int idx = dgvPreview.Rows.Add("", val, val);
                Color c = XTools.GetRandomColor();
                dgvPreview.Rows[idx].Cells[0].Style.BackColor = c;
                dgvPreview.Rows[idx].Tag = CreateThematic(c, 3); // 默认大小3
            }
        }

        private void GenerateGraduated()
        {
            if (cboField.SelectedIndex == -1) return;
            string fieldName = cboField.SelectedItem.ToString();
            int fieldIndex = _layer.GetFieldIndex(fieldName);

            // 1. 获取数值列表
            List<double> values = new List<double>();
            foreach (var f in _layer.Features)
            {
                try
                {
                    object obj = f.getAttribute(fieldIndex);
                    if (obj != null && double.TryParse(obj.ToString(), out double v))
                    {
                        values.Add(v);
                    }
                }
                catch { }
            }

            if (values.Count == 0)
            {
                MessageBox.Show("该字段没有有效的数值数据！");
                return;
            }

            int uniqueCount = values.Distinct().Count();
            int numClasses = (int)numClassCount.Value;

            if (uniqueCount < 2)
            {
                MessageBox.Show($"该字段的所有值都相同，无法进行分级。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (uniqueCount < numClasses)
            {
                MessageBox.Show($"唯一值数量 ({uniqueCount}) 少于分类级数 ({numClasses})。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Jenks 计算断点
            List<double> breaks = XTools.GetJenksBreaks(values, numClasses);
            if (breaks.Count < 2) return;

            // 3. 生成预览
            dgvPreview.Rows.Clear();
            Color startC = pnlStartColor.BackColor;
            Color endC = pnlEndColor.BackColor;
            float minS = (float)numMinSize.Value;
            float maxS = (float)numMaxSize.Value;

            for (int i = 0; i < breaks.Count - 1; i++)
            {
                double min = breaks[i];
                double max = breaks[i + 1];
                string label = $"{min:0.##} - {max:0.##}";

                double fraction = (double)i / (Math.Max(1, numClasses - 1));
                Color c = XTools.GetInterpolatedColor(startC, endC, fraction);
                float size = minS + (float)((maxS - minS) * fraction);

                int idx = dgvPreview.Rows.Add("", label, label);
                dgvPreview.Rows[idx].Cells[0].Style.BackColor = c;

                XThematic thematic = CreateThematic(c, size);
                dgvPreview.Rows[idx].Tag = thematic;
            }
        }

        private XThematic CreateThematic(Color c, float size)
        {
            if (_layer.ShapeType == SHAPETYPE.polygon)
            {
                return new XThematic(new Pen(Color.Black, 1), new Pen(Color.Cyan, 2), new Pen(Color.Black, 1), new SolidBrush(c), (int)size);
            }
            else if (_layer.ShapeType == SHAPETYPE.line)
            {
                return new XThematic(new Pen(c, size), new Pen(Color.Cyan, 2), new Pen(c, size), new SolidBrush(c), (int)size);
            }
            else // Point
            {
                return new XThematic(new Pen(Color.Black, 1), new Pen(Color.Cyan, 2), new Pen(Color.Black, 1), new SolidBrush(c), (int)size);
            }
        }

        private void PickColor(Panel p)
        {
            ColorDialog cd = new ColorDialog();
            if (cd.ShowDialog() == DialogResult.OK)
            {
                p.BackColor = cd.Color;
                if (cboMode.SelectedIndex == 0) UpdateSinglePreview();
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            _layer.ThematicMode = (RenderMode)cboMode.SelectedIndex;
            _layer.RenderField = cboField.SelectedIndex >= 0 ? cboField.SelectedItem.ToString() : "";

            if (_layer.ThematicMode == RenderMode.SingleSymbol)
            {
                Color c = pnlSingleColor.BackColor;
                float s = (float)numSingleSize.Value;
                _layer.UnselectedThematic = CreateThematic(c, s);
            }
            else if (_layer.ThematicMode == RenderMode.UniqueValues)
            {
                _layer.UniqueRenderer.Clear();
                foreach (DataGridViewRow row in dgvPreview.Rows)
                {
                    string val = row.Cells[1].Value.ToString();
                    Color c = (Color)row.Cells[0].Style.BackColor;
                    if (row.Tag is XThematic th)
                        _layer.UniqueRenderer[val] = th;
                    else
                        _layer.UniqueRenderer[val] = CreateThematic(c, 3);
                }
            }
            else if (_layer.ThematicMode == RenderMode.GraduatedSymbols)
            {
                _layer.ClassBreaks.Clear();
                foreach (DataGridViewRow row in dgvPreview.Rows)
                {
                    string[] parts = row.Cells[1].Value.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        double min = double.Parse(parts[0]);
                        double max = double.Parse(parts[1]);
                        XThematic th = row.Tag as XThematic;

                        _layer.ClassBreaks.Add(new ClassBreak()
                        {
                            MinValue = min,
                            MaxValue = max,
                            Thematic = th,
                            Label = row.Cells[2].Value.ToString()
                        });
                    }
                }
            }
        }
    }
}