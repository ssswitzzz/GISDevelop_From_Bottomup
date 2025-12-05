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
    public partial class FormAttribute : Form
    {
        private XVectorLayer _layer;
        public FormAttribute(XVectorLayer layer)
        {
            InitializeComponent();
            _layer = layer;
        }
        private void FormAttribute_Load(object sender, EventArgs e)
        {
            // 1. 添加表头（列名）
            // 参考 Lesson 10 PPT [cite: 805-808]
            for (int i = 0; i < _layer.Fields.Count; i++)
            {
                dgvValues.Columns.Add(_layer.Fields[i].name, _layer.Fields[i].name);
            }

            // 2. 添加行数据
            // 为了提高性能，我们不一个格子一个格子填，而是准备好一行的数据一次性添加
            for (int i = 0; i < _layer.FeatureCount(); i++)
            {
                XFeature feature = _layer.GetFeature(i);

                // 准备一行的数据数组
                object[] rowValues = new object[_layer.Fields.Count];

                for (int j = 0; j < _layer.Fields.Count; j++)
                {
                    // 获取属性值 [cite: 814]
                    rowValues[j] = feature.getAttribute(j);
                }

                // 添加整行
                dgvValues.Rows.Add(rowValues);
            }

            // 设置标题显示记录数
            this.Text = $"属性表 - {_layer.Name} (共 {_layer.FeatureCount()} 条记录)";
        }
    }
}
