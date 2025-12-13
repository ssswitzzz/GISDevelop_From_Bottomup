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

        private ComboBox cboMode;
        private ComboBox cboField;
        private Panel pnlContainer; 
        private DataGridView dgvPreview;
        private Button btnOK;

        private Panel pnlSingleColor;
        private NumericUpDown numSingleSize;

        private Button btnGenerateUnique;

        private NumericUpDown numClassCount;
        private Panel pnlStartColor;
        private Panel pnlEndColor;
        private NumericUpDown numMinSize;
        private NumericUpDown numMaxSize;

        private Button _btnCalcGraduated;

        public FormSymbology(XVectorLayer layer)
        {
            _layer = layer;
            InitializeComponent(); 
            SetupCustomUI();
            LoadLayerData();
        }

        private void SetupCustomUI()
        {
            this.Text = $"符号系统设置 - {_layer.Name}";
            this.Size = new Size(750, 680); 
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int padding = 30; 
            int y = 30;

            Label lblMode = new Label() { Text = "渲染模式:", Location = new Point(padding, y + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
            cboMode = new ComboBox() { Location = new Point(140, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("微软雅黑", 9) };
            cboMode.Items.AddRange(new object[] { "单一符号", "唯一值", "分级符号" });
            cboMode.SelectedIndexChanged += CboMode_SelectedIndexChanged;
            this.Controls.Add(lblMode);
            this.Controls.Add(cboMode);

            Label lblField = new Label() { Text = "分类字段:", Location = new Point(400, y + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
            cboField = new ComboBox() { Location = new Point(500, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("微软雅黑", 9) };
            this.Controls.Add(lblField);
            this.Controls.Add(cboField);

            y += 50;

            pnlContainer = new Panel() { Location = new Point(padding, y), Size = new Size(675, 130), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke };
            this.Controls.Add(pnlContainer);

            y += 150; 

   
            Label lblPreviewTitle = new Label() { Text = "符号预览:", Location = new Point(padding, y), AutoSize = true };
            this.Controls.Add(lblPreviewTitle);
            y += 25;

            dgvPreview = new DataGridView()
            {
                Location = new Point(padding, y),
                Size = new Size(675, 300), 
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing, // 1. 允许自定义高度
                ColumnHeadersHeight = 40, 
            };

            dgvPreview.Columns.Add("Symbol", "符号颜色");
            dgvPreview.Columns.Add("Value", "数值范围 / 类别");
            dgvPreview.Columns.Add("Label", "图例标签");


            dgvPreview.Columns[0].Width = 100;
            dgvPreview.Columns[1].Width = 280; 
            dgvPreview.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            this.Controls.Add(dgvPreview);


            int btnY = this.ClientSize.Height - 60;
            btnOK = new Button() { Text = "确定应用", DialogResult = DialogResult.OK, Location = new Point(this.ClientSize.Width - 240, btnY), Width = 100, Height = 35, Cursor = Cursors.Hand };
            Button btnCancel = new Button() { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(this.ClientSize.Width - 120, btnY), Width = 100, Height = 35, Cursor = Cursors.Hand };
            btnOK.Click += BtnOK_Click;

            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);


            InitSingleControls();
            InitUniqueControls();
            InitGraduatedControls();
        }

        private Color GetThematicColor(XThematic th)
        {
            if (th == null) return Color.Black;

            if (_layer.ShapeType == SHAPETYPE.point)
            {
                return th.PointBrush.Color; 
            }
            else if (_layer.ShapeType == SHAPETYPE.line)
            {
                return th.LinePen.Color; 
            }
            else // Polygon
            {
                return th.PolygonBrush.Color;
            }
        }

        private decimal GetThematicSize(XThematic th)
        {
            if (th == null) return 3;

            if (_layer.ShapeType == SHAPETYPE.point)
            {
                return (decimal)th.PointRadius;
            }
            else if (_layer.ShapeType == SHAPETYPE.line)
            {
                return (decimal)th.LinePen.Width;
            }
            else 
            {
              
                return (decimal)th.PointRadius;
            }
        }

        private void InitSingleControls()
        {

            Label l1 = new Label() { Text = "符号颜色:", Location = new Point(30, 50), AutoSize = true };
            pnlSingleColor = new Panel() { Location = new Point(140, 45), Size = new Size(80, 25), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Red, Cursor = Cursors.Hand };
            pnlSingleColor.Click += (s, e) => PickColor(pnlSingleColor);


            Label l2 = new Label() { Text = "符号大小:", Location = new Point(380, 50), AutoSize = true };
            numSingleSize = new NumericUpDown() { Location = new Point(480, 48), Minimum = 1, Maximum = 50, Value = 3, Width = 80 };
        }

        private void InitUniqueControls()
        {

            btnGenerateUnique = new Button() { Text = "生成所有唯一值分类", Location = new Point(220, 45), Width = 220, Height = 40, Cursor = Cursors.Hand };
            btnGenerateUnique.Click += (s, e) => GenerateUniqueValues();
        }

        private void InitGraduatedControls()
        {
 

            numClassCount = new NumericUpDown() { Location = new Point(140, 18), Minimum = 2, Maximum = 20, Value = 5, Width = 80 };

            pnlStartColor = new Panel() { Location = new Point(140, 55), Size = new Size(50, 25), BackColor = Color.LightYellow, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            pnlStartColor.Click += (s, e) => PickColor(pnlStartColor);

            pnlEndColor = new Panel() { Location = new Point(230, 55), Size = new Size(50, 25), BackColor = Color.DarkRed, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            pnlEndColor.Click += (s, e) => PickColor(pnlEndColor);

            numMinSize = new NumericUpDown() { Location = new Point(480, 58), Minimum = 1, Maximum = 50, Value = 3, Width = 60 }; // X=460
            numMaxSize = new NumericUpDown() { Location = new Point(570, 58), Minimum = 1, Maximum = 100, Value = 15, Width = 60 };

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

            if (!string.IsNullOrEmpty(_layer.RenderField) && cboField.Items.Contains(_layer.RenderField))
            {
                cboField.SelectedItem = _layer.RenderField;
            }
            else if (cboField.Items.Count > 0)
            {
                cboField.SelectedIndex = 0;
            }
            cboMode.SelectedIndex = (int)_layer.ThematicMode;
        }

        private void CboMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlContainer.Controls.Clear();
            dgvPreview.Rows.Clear();
            cboField.Enabled = true;

            int mode = cboMode.SelectedIndex;

            bool isRestoring = (mode == (int)_layer.ThematicMode);

            if (mode == 0) 
            {
                cboField.Enabled = false;

                pnlContainer.Controls.Add(new Label() { Text = "符号颜色:", Location = new Point(30, 50), AutoSize = true });
                pnlContainer.Controls.Add(pnlSingleColor);
                pnlContainer.Controls.Add(new Label() { Text = "符号大小:", Location = new Point(380, 50), AutoSize = true });
                pnlContainer.Controls.Add(numSingleSize);


                if (isRestoring && _layer.UnselectedThematic != null)
                {
                    pnlSingleColor.BackColor = GetThematicColor(_layer.UnselectedThematic);
                    numSingleSize.Value = GetThematicSize(_layer.UnselectedThematic);
                }
                else
                {
                    // 如果没有记忆，给个默认值
                    pnlSingleColor.BackColor = Color.Red;
                    numSingleSize.Value = 3;
                }

                dgvPreview.Rows.Add("", "所有要素", "单一符号");
                UpdateSinglePreview();
            }
            else if (mode == 1)
            {
                pnlContainer.Controls.Add(btnGenerateUnique);


                if (isRestoring && _layer.UniqueRenderer != null && _layer.UniqueRenderer.Count > 0)
                {
                    foreach (var kvp in _layer.UniqueRenderer)
                    {
                        string val = kvp.Key;
                        XThematic th = kvp.Value;
                        Color c = GetThematicColor(th);

                        int idx = dgvPreview.Rows.Add("", val, val);
                        dgvPreview.Rows[idx].Cells[0].Style.BackColor = c;
                        dgvPreview.Rows[idx].Tag = th; 
                    }
                }
            }
            else if (mode == 2) // Graduated Symbols (分级符号)
            {
                pnlContainer.Controls.Add(new Label() { Text = "分类级数:", Location = new Point(30, 20), AutoSize = true });
                pnlContainer.Controls.Add(numClassCount);
                pnlContainer.Controls.Add(new Label() { Text = "颜色色带:", Location = new Point(30, 60), AutoSize = true });
                pnlContainer.Controls.Add(pnlStartColor);
                pnlContainer.Controls.Add(new Label() { Text = "至", Location = new Point(200, 60), AutoSize = true });
                pnlContainer.Controls.Add(pnlEndColor);
                pnlContainer.Controls.Add(new Label() { Text = "尺寸范围:", Location = new Point(380, 60), AutoSize = true });
                pnlContainer.Controls.Add(numMinSize);
                pnlContainer.Controls.Add(new Label() { Text = "-", Location = new Point(550, 60), AutoSize = true });
                pnlContainer.Controls.Add(numMaxSize);
                pnlContainer.Controls.Add(_btnCalcGraduated);

                if (isRestoring && _layer.ClassBreaks != null && _layer.ClassBreaks.Count > 0)
                {
                    numClassCount.Value = _layer.ClassBreaks.Count;
                    if (_layer.ClassBreaks.Count > 1)
                    {
                        pnlStartColor.BackColor = GetThematicColor(_layer.ClassBreaks[0].Thematic);
                        pnlEndColor.BackColor = GetThematicColor(_layer.ClassBreaks[_layer.ClassBreaks.Count - 1].Thematic);
                    }

                    foreach (var cb in _layer.ClassBreaks)
                    {
                        Color c = GetThematicColor(cb.Thematic); 
                        int idx = dgvPreview.Rows.Add("", cb.Label, cb.Label);
                        dgvPreview.Rows[idx].Cells[0].Style.BackColor = c;
                        dgvPreview.Rows[idx].Tag = cb.Thematic;
                    }
                }
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

            List<double> breaks = XTools.GetJenksBreaks(values, numClasses);
            if (breaks.Count < 2) return;

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
            // 根据图层类型，把 Color 和 Size 塞到正确的属性里
            if (_layer.ShapeType == SHAPETYPE.polygon)
            {
                return new XThematic(
                    new Pen(Color.Black, 1),  
                    new Pen(Color.Cyan, 2), 
                    new Pen(Color.Black, 1),  
                    new SolidBrush(c),        
                    (int)size                
                );
            }
            else if (_layer.ShapeType == SHAPETYPE.line)
            {
                return new XThematic(
                    new Pen(c, size),         
                    new Pen(Color.Cyan, 2),  
                    new Pen(c, size),       
                    new SolidBrush(c),   
                    (int)size           
                );
            }
            else
            {
                return new XThematic(
                    new Pen(Color.Black, 1),   
                    new Pen(Color.Cyan, 2),     
                    new Pen(Color.Black, 1),     
                    new SolidBrush(c),         
                    (int)size             
                );
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