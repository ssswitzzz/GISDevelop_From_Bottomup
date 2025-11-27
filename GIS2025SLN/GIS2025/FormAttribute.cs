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
        public FormAttribute(XVectorLayer layer)
        {
            InitializeComponent();
            //根据图层的fields添加列
            for (int i = 0; i < layer.Fields.Count; i++)
            {
                dgvValues.Columns.Add(layer.Fields[i].name, layer.Fields[i].name);
            }

            //添加行，来自于layer的属性值
            for (int i = 0; i < layer.FeatureCount(); i++)
            {
                int index=dgvValues.Rows.Add();
                for (int j = 0; j < layer.Fields.Count; j++)
                {
                    dgvValues.Rows[index].Cells[j].Value = layer.GetFeature(i).getAttribute(j);
                }
            }
        }

    }
}
