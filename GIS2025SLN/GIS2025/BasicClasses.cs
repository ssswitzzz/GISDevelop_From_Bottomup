using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.LinkLabel;

namespace XGIS
{
    public class XThematic
    {
        //线实体显示样式
        public Pen LinePen = new Pen(Color.Black, 1);
        //面实体显示样式
        public Pen PolygonPen = new Pen(Color.Blue, 1);
        public SolidBrush PolygonBrush = new SolidBrush(Color.Yellow);
        //点实体显示样式
        public Pen PointPen = new Pen(Color.Red, 1);
        public SolidBrush PointBrush = new SolidBrush(Color.White);
        public int PointRadius = 5;

        public XThematic()
        {
        }

        public XThematic(Pen _LinePen,
            Pen _PolygonPen, SolidBrush _PolygonBrush,
            Pen _PointPen, SolidBrush _PointBrush, int _PointRadius)
        {
            LinePen = _LinePen;
            PolygonPen = _PolygonPen;
            PolygonBrush = _PolygonBrush;
            PointPen = _PointPen;
            PointBrush = _PointBrush;
            PointRadius = _PointRadius;
        }
    }

    public class XSelect
    {
        public class SelectResult
        {
            public XFeature feature;
            public double criterion;
            public SelectResult(XFeature _feature, double _criterion)
            {
                feature = _feature;
                criterion = _criterion;
            }
        }

        public static List<XFeature> ToFeatures(List<SelectResult> srs)
        {
            List<XFeature> fs = new List<XFeature>();
            foreach(SelectResult sr in srs)
            {
                fs.Add(sr.feature);
            }
            return fs;
        }

        public static List<SelectResult> SelectFeaturesByExtent(XExtent extent, 
            List<XFeature> features)
        {
            List<SelectResult> selection = new List<SelectResult>();
            foreach (XFeature feature in features)
            {
                if (extent.Includes(feature.spatial.extent))
                    selection.Add(new SelectResult(feature, 0));
            }
            return selection;
        }

        public static List<SelectResult> SelectFeaturesByVertex(
            XVertex vertex, List<XFeature> features, double tolerance)
        {
            List<SelectResult> selection = new List<SelectResult>();
            XExtent extent = new XExtent(vertex.x - tolerance, vertex.x + tolerance,
                vertex.y - tolerance, vertex.y + tolerance);




            foreach (XFeature feature in features)
            {
                if (!extent.IntersectOrNot(feature.spatial.extent))                     
                    continue;

                double distance = feature.spatial.Distance(vertex);
                if (distance <= tolerance)
                    selection.Add(new SelectResult(feature, distance));
            }
            selection.Sort((x, y) => x.criterion.CompareTo(y.criterion));


            return selection;
        }
    }

    public class XMyFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct MyFileHeader
        {
            public double MinX, MinY, MaxX, MaxY;
            public int FeatureCount, ShapeType, FieldCount;
        };

        static void WriteFileHeader(XVectorLayer layer, BinaryWriter bw)
        {
            MyFileHeader mfh = new MyFileHeader();
            mfh.MinX = layer.Extent.GetMinX();
            mfh.MinY = layer.Extent.GetMinY();
            mfh.MaxX = layer.Extent.GetMaxX();
            mfh.MaxY = layer.Extent.GetMaxY();
            mfh.FeatureCount = layer.FeatureCount();
            mfh.ShapeType = (int)(layer.ShapeType);
            mfh.FieldCount = layer.Fields.Count;
            bw.Write(XTools.FromStructToBytes(mfh));
        }

        static List<Type> AllTypes = new List<Type>{            
            typeof(bool),
            typeof(byte),
            typeof(char),
            typeof(decimal),
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(long),
            typeof(sbyte),
            typeof(short),
            typeof(string),
            typeof(uint),
            typeof(ulong),
            typeof(ushort)
        };

        static void WriteFields(List<XField> fields, BinaryWriter bw)
        {
            for (int fieldindex = 0; fieldindex < fields.Count; fieldindex++)
            {
                XField field = fields[fieldindex];
                bw.Write(AllTypes.IndexOf(field.datatype));
                XTools.WriteString(field.name, bw);
            }
        }

        static List<XField> ReadFields(BinaryReader br, int FieldCount)
        {
            List<XField> fields = new List<XField>();
            for (int fieldindex = 0; fieldindex < FieldCount; fieldindex++)
            {
                Type fieldtype = AllTypes[br.ReadInt32()];
                string fieldname = XTools.ReadString(br);
                fields.Add(new XField(fieldtype, fieldname));
            }
            return fields;
        }


        static void WriteMultipleVertexes(List<XVertex> vs, BinaryWriter bw)
        {
            bw.Write(vs.Count);
            for (int i = 0; i < vs.Count; i++)
            {
                vs[i].Write(bw);
            }
        }

        static List<XVertex> ReadMultipleVertexes(BinaryReader br)
        {
            List<XVertex> vs = new List<XVertex>();
            int vcount = br.ReadInt32();
            for (int vc = 0; vc < vcount; vc++)
                vs.Add(new XVertex(br));
            return vs;
        }


        static void WriteFeatures(XVectorLayer layer, BinaryWriter bw)
        {
            for (int i = 0; i < layer.FeatureCount(); i++)
            {
                XFeature feature = layer.GetFeature(i);
                WriteMultipleVertexes(feature.spatial.vertexes, bw);
                feature.attribute.Write(bw);
            }
        }

        static void ReadFeatures(XVectorLayer layer, 
            BinaryReader br,
            int FeatureCount)
        {
            for (int featureindex = 0; featureindex < FeatureCount; featureindex++)
            {
                List<XVertex> vs = ReadMultipleVertexes(br);
                XAttribute attribute = new XAttribute(layer.Fields, br);
                XSpatial spatial = null;
                if (layer.ShapeType == SHAPETYPE.point)
                    spatial = new XPointSpatial(vs[0]);
                else if (layer.ShapeType == SHAPETYPE.line)
                    spatial = new XLineSpatial(vs);
                else if (layer.ShapeType == SHAPETYPE.polygon)
                    spatial = new XPolygonSpatial(vs);
                XFeature feature = new XFeature(spatial, attribute);
                layer.AddFeature(feature);
            }
        }



        public static void WriteFile(XVectorLayer layer, string filename)
        {
            FileStream fsr = new FileStream(filename, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fsr);

            //写文件头
            WriteFileHeader(layer, bw);
            //写图层名
            XTools.WriteString(layer.Name, bw);
            //写字段信息
            WriteFields(layer.Fields, bw);
            //写空间对象
            WriteFeatures(layer, bw);

            //其它内容
            bw.Close();
            fsr.Close();
        }

        public static XVectorLayer ReadFile(string filename)
        {
            FileStream fsr = new FileStream(filename, FileMode.Open);
            BinaryReader br = new BinaryReader(fsr);
            //读文件头
            MyFileHeader mfh = (MyFileHeader)(XTools.FromBytes2Struct(br, typeof(MyFileHeader)));
            SHAPETYPE ShapeType = (SHAPETYPE)Enum.Parse(typeof(SHAPETYPE), mfh.ShapeType.ToString());
            //读图层名称
            string layername = XTools.ReadString(br);
            //构造空的图层
            XVectorLayer layer = new XVectorLayer(layername, ShapeType);
            //读字段
            layer.Fields = ReadFields(br, mfh.FieldCount);
            //定义图层范围
            layer.Extent = new XExtent(mfh.MinX, mfh.MaxX, mfh.MinY, mfh.MaxY);
            //读控件对象
            ReadFeatures(layer, br, mfh.FeatureCount);
            br.Close();
            fsr.Close();
            return layer;
        }

    }




    public class XShapefile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct ShapefileHeader
        {
            public int Unused1, Unused2, Unused3, Unused4;
            public int Unused5, Unused6, Unused7, Unused8;
            public int ShapeType;
            public double Xmin;
            public double Ymin;
            public double Xmax;
            public double Ymax;
            public double Unused9, Unused10, Unused11, Unused12;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct RecordHeader
        {
            public int RecordNumber;
            public int RecordLength;
            public int ShapeType;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct DBFHeader
        {
            public byte FileType, Year, Month, Day;
            public int RecordCount;
            public short HeaderLength, RecordLength;
            public long Unused1, Unused2;
            public int Unused3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DBFField
        {
            public byte b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11;
            public byte FieldType;
            public int DisplacementInRecord;
            public byte LengthOfField;
            public byte NumberOfDecimalPlaces;
            public long Unused1;
            public int Unused2;
            public short Unused3;
        }


        static List<XField> ReadDBFFields(string dbffilename)
        {
            FileStream fsr = new FileStream(dbffilename, FileMode.Open);
            BinaryReader br = new BinaryReader(fsr);
            DBFHeader dh = (DBFHeader)XTools.FromBytes2Struct(br, typeof(DBFHeader));
            int FieldCount = (dh.HeaderLength - 33) / 32;
            List<XField> fields = new List<XField>();
            for (int i = 0; i < FieldCount; i++)
                fields.Add(new XField(br));
            br.Close();
            fsr.Close();
            return fields;
        }

        static List<XAttribute> ReadDBFValues(string dbffilename, List<XField> fields)
        {
            FileStream fsr = new FileStream(dbffilename, FileMode.Open);
            BinaryReader br = new BinaryReader(fsr);
            //文件头
            DBFHeader dh = (DBFHeader)XTools.FromBytes2Struct(br, typeof(DBFHeader));
            //字段区和结束标志
            int FieldCount = (dh.HeaderLength - 33) / 32;
            br.ReadBytes(32 * FieldCount + 1); //跳过字段区及结束标志字节
            
            //读实际的属性值
            List<XAttribute> attributes = new List<XAttribute>();
            for (int i = 0; i < dh.RecordCount; i++) //开始读取具体数值
            {
                XAttribute attribute = new XAttribute();
                char tempchar = (char)br.ReadByte();  //每个记录的开始都有一个起始字节
                for (int j = 0; j < FieldCount; j++)
                    attribute.AddValue(fields[j].DBFValueToObject(br));
                attributes.Add(attribute);
            }
            br.Close();
            fsr.Close();
            return attributes;
        }


        static ShapefileHeader ReadFileHeader(BinaryReader br)
        {
            return (ShapefileHeader)XTools.FromBytes2Struct(br, typeof(ShapefileHeader));
        }

        static RecordHeader ReadRecordHeader(BinaryReader br)
        {
            return (RecordHeader)XTools.FromBytes2Struct(br, typeof(RecordHeader));
        }


        public static XVectorLayer ReadShapefile(string shpfilename)
        {
            FileStream fsr = new FileStream(shpfilename, FileMode.Open);
            BinaryReader br = new BinaryReader(fsr);


     
            ShapefileHeader sfh = ReadFileHeader(br);
            SHAPETYPE ShapeType = Int2Shapetype[sfh.ShapeType];
            XVectorLayer layer = new XVectorLayer(shpfilename, ShapeType);
            layer.Extent = new XExtent(sfh.Xmax, sfh.Xmin, sfh.Ymax, sfh.Ymin);

            string dbffilename = shpfilename.ToLower().Replace(".shp", ".dbf");
            layer.Fields = ReadDBFFields(dbffilename);
            List<XAttribute> attributes = ReadDBFValues(dbffilename, layer.Fields);

            int index = 0;

            //其他代码
            while (br.PeekChar() != -1)
            {
                //读记录头
                RecordHeader rh = ReadRecordHeader(br);
                int ByteLength = XTools.ReverseInt(rh.RecordLength) * 2 - 4;
                //一次性把记录内容读出来
                byte[] RecordContent = br.ReadBytes(ByteLength);

                if (ShapeType == SHAPETYPE.point)
                {
                    XPointSpatial onepoint = ReadPoint(RecordContent);
                    XAttribute attribute = attributes[index];
                    index++;
                    XFeature feature = new XFeature(onepoint, attribute);
                    layer.AddFeature(feature);
                }
                else if (ShapeType == SHAPETYPE.line)
                {
                    List<XLineSpatial> lines = ReadLines(RecordContent);
                    XAttribute attribute = attributes[index];
                    index++;
                    foreach (XLineSpatial line in lines)
                    {
                        XFeature feature = new XFeature(line, new XAttribute(attribute));
                        layer.AddFeature(feature);
                    }
                }
                else if (ShapeType == SHAPETYPE.polygon)
                {
                    List<XPolygonSpatial> polygons = ReadPolygons(RecordContent);
                    XAttribute attribute = attributes[index];
                    index++;
                    foreach (XPolygonSpatial polygon in polygons)
                    {
                        XFeature feature = new XFeature(polygon, attribute);
                        layer.AddFeature(feature);
                    }
                }
                //其他代码
            }
            br.Close();
            fsr.Close();
            return layer;
        }

        static XPointSpatial ReadPoint(byte[] RecordContent)
        {
            double x = BitConverter.ToDouble(RecordContent, 0);
            double y = BitConverter.ToDouble(RecordContent, 8);
            return new XPointSpatial(new XVertex(x, y));
        }
        static List<XLineSpatial> ReadLines(byte[] RecordContent)
        {
            int N = BitConverter.ToInt32(RecordContent, 32);
            int M = BitConverter.ToInt32(RecordContent, 36);

            int[] parts = new int[N + 1];

            for (int i = 0; i < N; i++)
            {
                parts[i] = BitConverter.ToInt32(RecordContent, 40 + i * 4);
            }
            parts[N] = M;
            List<XLineSpatial> lines = new List<XLineSpatial>();
            for (int i = 0; i < N; i++)
            {
                List<XVertex> vertexes = new List<XVertex>();
                for (int j = parts[i]; j < parts[i + 1]; j++)
                {
                    double x = BitConverter.ToDouble(RecordContent, 40 + N * 4 + j * 16);
                    double y = BitConverter.ToDouble(RecordContent, 40 + N * 4 + j * 16 + 8);
                    vertexes.Add(new XVertex(x, y));
                }
                lines.Add(new XLineSpatial(vertexes));
            }
            return lines;
        }

        static List<XPolygonSpatial> ReadPolygons(byte[] RecordContent)
        {
            int N = BitConverter.ToInt32(RecordContent, 32);
            int M = BitConverter.ToInt32(RecordContent, 36);
            int[] parts = new int[N + 1];
            for (int i = 0; i < N; i++)
            {
                parts[i] = BitConverter.ToInt32(RecordContent, 40 + i * 4);
            }
            parts[N] = M;
            List<XPolygonSpatial> polygons = new List<XPolygonSpatial>();
            for (int i = 0; i < N; i++)
            {
                List<XVertex> vertexes = new List<XVertex>();
                for (int j = parts[i]; j < parts[i + 1]; j++)
                {
                    double x = BitConverter.ToDouble(RecordContent, 40 + N * 4 + j * 16);
                    double y = BitConverter.ToDouble(RecordContent, 40 + N * 4 + j * 16 + 8);
                    vertexes.Add(new XVertex(x, y));
                }
                polygons.Add(new XPolygonSpatial(vertexes));
            }
            return polygons;
        }

        static Dictionary<int, SHAPETYPE> Int2Shapetype = new Dictionary<int, SHAPETYPE>
        {
            {1, SHAPETYPE.point },
            {3, SHAPETYPE.line },
            {5, SHAPETYPE.polygon }
        };
    }

    public class XTools
    {
        //distance between Point C and segment AB
        public static double DistanceBetweenPointAndSegment(
            XVertex A, XVertex B, XVertex C)
        {
            if (A.IsSame(B)) return B.Distance(C);
            double dot1 = DotProduct(A, B, C);
            if (dot1 > 0) return B.Distance(C);
            double dot2 = DotProduct(B, A, C);
            if (dot2 > 0) return A.Distance(C);
            double dist = CrossProduct(A, B, C) / A.Distance(B);
            return Math.Abs(dist);
        }

        static double DotProduct(XVertex A, XVertex B, XVertex C)
        {
            XVertex AB = new XVertex(B.x - A.x, B.y - A.y);
            XVertex BC = new XVertex(C.x - B.x, C.y - B.y);
            return AB.x * BC.x + AB.y * BC.y;
        }

        static double CrossProduct(XVertex A, XVertex B, XVertex C)
        {
            XVertex AB = new XVertex(B.x - A.x, B.y - A.y);
            XVertex AC = new XVertex(C.x - A.x, C.y - A.y);
            return VectorProduct(AB, AC);
        }

        public static string BytesToString(byte[] byteArray)
        {
            int count = byteArray.Length;
            for (int i = 0; i < byteArray.Length; i++)
            {
                if (byteArray[i] == 0)
                {
                    count = i;
                    break;
                }
            }
            return Encoding.GetEncoding("gb2312").GetString(byteArray, 0, count);
        }

        public static double CalculateArea(List<XVertex> _vertexes)
        {
            double area = 0;
            for (int i = 0; i < _vertexes.Count - 1; i++)
            {
                area += VectorProduct(_vertexes[i], _vertexes[i + 1]);
            }
            area += VectorProduct(_vertexes[_vertexes.Count - 1], _vertexes[0]);
            return area / 2;
        }

        public static double VectorProduct(XVertex v1, XVertex v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }

        public static double CalculateLength(List<XVertex> _vertexes)
        {
            double length = 0;
            for (int i = 0; i < _vertexes.Count - 1; i++)
            {
                length += _vertexes[i].Distance(_vertexes[i + 1]);
            }
            return length;
        }

        /// <summary>
        /// 从文件中读取字节数组，根据输入的结构体定义，生成一个有内容的结构体实例
        /// </summary>
        /// <param name="br"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Object FromBytes2Struct(BinaryReader br, Type type)
        {
            byte[] buff = br.ReadBytes(Marshal.SizeOf(type));
            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);
            Object result = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), type);
            handle.Free();
            return result;
        }

        public static int ReverseInt(int value)
        {
            byte[] barray = BitConverter.GetBytes(value);
            Array.Reverse(barray);
            return BitConverter.ToInt32(barray, 0);
        }

        internal static byte[] FromStructToBytes(object struc)
        {
            byte[] bytes = new byte[Marshal.SizeOf(struc.GetType())];
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(struc, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return bytes;
        }

        public static void WriteString(string s, BinaryWriter bw)
        {
            byte[] sbytes = Encoding.GetEncoding("gb2312").GetBytes(s);
            bw.Write(sbytes.Length);
            bw.Write(sbytes);
        }

        public static string ReadString(BinaryReader br)
        {
            int length = br.ReadInt32();
            byte[] sbytes = br.ReadBytes(length);
            return Encoding.GetEncoding("gb2312").GetString(sbytes);
        }




    }



    public class XVectorLayer
    {
        public string Name;
        public SHAPETYPE ShapeType;
        public List<XFeature> Features = new List<XFeature>();
        public XExtent Extent;
        public List<XField> Fields = new List<XField>();
        public bool LabelOrNot = true;
        public int LabelIndex = 0;

        public List<XFeature> SelectedFeatures = new List<XFeature>();
        public XThematic UnselectedThematic, SelectedThematic;

        public XVectorLayer(string _name, SHAPETYPE _shapetype)
        {
            Name = _name;
            ShapeType = _shapetype;

            UnselectedThematic = new XThematic();

            SelectedThematic = new XThematic(new Pen(Color.Red, 1),
                new Pen(Color.Red, 1), new SolidBrush(Color.Pink),
                new Pen(Color.Red, 1), new SolidBrush(Color.Pink), 5);

        }



        public void SelectByVertex(XVertex vertex, double tolerance, bool modify)
        {
            List<XFeature> fs = XSelect.ToFeatures(
                XSelect.SelectFeaturesByVertex(vertex, Features, tolerance));
            ModifySelection(fs, modify);
        }
            
        

        public void SelectByExtent(XExtent extent, bool modify)
        {
            List<XFeature> fs = XSelect.ToFeatures(
                XSelect.SelectFeaturesByExtent(extent, Features));
            ModifySelection(fs, modify);
        }

        private void ModifySelection(List<XFeature> features, bool modify)
        {
            if (modify == false)
            {
                SelectedFeatures = features;
                return;
            }

            bool IncludeAll = true;
            foreach (XFeature feature in features)
            {
                if (!SelectedFeatures.Contains(feature))
                {
                    //情景2：添加入选择集
                    IncludeAll = false;
                    SelectedFeatures.Add(feature);
                }
            }
            if (IncludeAll)
            {
                //情景1：从选择集中移出
                foreach (XFeature feature in features)
                {
                    SelectedFeatures.Remove(feature);
                }
            }
        }


        public void UpdateExtent()
        {
            if (Features.Count == 0)
                Extent = null;
            else
            {
                Extent = new XExtent(Features[0].spatial.extent);
                for (int i = 1; i < Features.Count; i++)
                    Extent.Merge(Features[i].spatial.extent);
            }
        }
        public void AddFeature(XFeature feature)
        {
            Features.Add(feature);
            if (Features.Count == 1)
                Extent = new XExtent(feature.spatial.extent);
            else
                Extent.Merge(feature.spatial.extent);
        }

        public void RemoveFeature(int index)
        {
            if (index >= Features.Count) return;
            Features.RemoveAt(index);
            UpdateExtent();
        }

        public void RemoveFeature(XFeature feature)
        {
            if (Features.Remove(feature))
                UpdateExtent();
        }

        public int FeatureCount()
        {
            return Features.Count;
        }

        public XFeature GetFeature(int index)
        {
            if (index>= Features.Count) return null;
            return Features[index];
        }

        public void Clear()
        {
            Features.Clear();
            UpdateExtent();
        }

        public void draw(Graphics graphics, XView view)
        {
            if (Extent == null) return;
            if (Features.Count==0) return;
            if (FeatureCount() == 0) return;

            if (!Extent.IntersectOrNot(view.CurrentMapExtent)) return;



            for (int i = 0; i < Features.Count; i++)
            {
                if (Features[i].spatial.extent.IntersectOrNot(view.CurrentMapExtent))
                    Features[i].draw(graphics, view, LabelOrNot, LabelIndex,
                        SelectedFeatures.Contains(Features[i])?
                            SelectedThematic:
                            UnselectedThematic);
            }
        }

    }
    public class XField
    {
        public Type datatype;
        public string name;

        public int DBFFieldLength;

        public XField(BinaryReader br)
        {
            XShapefile.DBFField dbfField = (XShapefile.DBFField)
                XTools.FromBytes2Struct(br, typeof(XShapefile.DBFField));

            DBFFieldLength = dbfField.LengthOfField;


            byte[] bs = new byte[] {dbfField.b1,dbfField.b2,dbfField.b3, dbfField.b4,dbfField.b5,
                dbfField.b6,dbfField.b7,dbfField.b8,dbfField.b9,dbfField.b10,dbfField.b11};



            name = XTools.BytesToString(bs).Trim();


            switch ((char)dbfField.FieldType)
            {
                case 'N':
                    if (dbfField.NumberOfDecimalPlaces == 0)
                        datatype = typeof(int);// Type.GetType("System.Int32");
                    else
                        datatype = Type.GetType("System.Double");
                    break;
                case 'F':
                    datatype = Type.GetType("System.Double");
                    break;
                default:
                    datatype = Type.GetType("System.String");
                    break;
            }
        }

        public XField(Type type, string name)
        {
            datatype = type;
            this.name = name;
        }

        public object DBFValueToObject(BinaryReader br)
        {
            byte[] temp = br.ReadBytes(DBFFieldLength);
            string sv = XTools.BytesToString(temp).Trim();
            if (datatype == Type.GetType("System.String"))
                return sv;
            else if (datatype == Type.GetType("System.Double"))
                return double.Parse(sv);
            else if (datatype == Type.GetType("System.Int32"))
                return int.Parse(sv);
            return sv;
        }

    }

    public enum SHAPETYPE
    {
        point, line, polygon, unknown
    }

    public enum XExploreActions
    {
        zoomin, zoomout, select,
        moveup, movedown, moveleft, moveright,
        zoominbybox, pan, noaction
    };

    public class XView
    {
        public XExtent CurrentMapExtent;
        Rectangle MapWindowSize;

        double MapMinX, MapMinY;
        int WinW, WinH;
        double MapW, MapH;
        double ScaleX, ScaleY;


        public XView(XExtent _Extent, Rectangle _Rectangle)
        {
          Update(_Extent, _Rectangle);
        }
        public void Update(XExtent _extent, Rectangle _rectangle)
        {
            //给地图窗口赋值
            MapWindowSize = _rectangle;
            //计算地图窗口的宽度
            WinW = MapWindowSize.Width;
            //计算地图窗口的高度
            WinH = MapWindowSize.Height;
            //计算比例尺
            ScaleX = ScaleY = Math.Max(_extent.GetWidth() / WinW, _extent.GetHeight() / WinH);
            //根据比例尺计算实际的地图范围的宽度
            MapW = ScaleX * WinW;
            //根据比例尺计算实际的地图范围的高度
            MapH = ScaleY * WinH;
            //获得地图范围中心
            XVertex center = _extent.GetCenter();
            //根据地图范围的中心，计算最小坐标极值
            MapMinX = center.x - MapW / 2;
            MapMinY = center.y - MapH / 2;
            //计算当前显示的实际地图范围
            CurrentMapExtent = new XExtent(
                MapMinX, 
                MapMinX + MapW, 
                MapMinY, 
                MapMinY + MapH);
        }

        public void Update1(XExtent _Extent, Rectangle _Rectangle)
        {
            CurrentMapExtent = _Extent;
            MapWindowSize = _Rectangle;

            MapMinX = CurrentMapExtent.GetMinX();
            MapMinY = CurrentMapExtent.GetMinY();
            WinW = MapWindowSize.Width;
            WinH = MapWindowSize.Height;
            MapW = CurrentMapExtent.GetWidth();
            MapH = CurrentMapExtent.GetHeight();
            ScaleX = MapW / WinW;
            ScaleY = MapH / WinH;
            ScaleX = ScaleY;
        }
        public void ChangeView(XExploreActions action)
        {
            CurrentMapExtent.ChangeExtent(action);
            Update(CurrentMapExtent, MapWindowSize);
        }
        public Point ToScreenPoint(XVertex onevertex)
        {
            double ScreenX = (onevertex.x - MapMinX) / ScaleX;
            double ScreenY = WinH - (onevertex.y - MapMinY) / ScaleY;
            return new Point((int)ScreenX, (int)ScreenY);
        }

        public List<Point> ToScreenPoints(List<XVertex> vs)
        {
            List<Point> points = new List<Point>();
            foreach (XVertex v in vs)
            {
                points.Add(ToScreenPoint(v));
            }
            return points;
        }

        public XVertex ToMapVertex(Point point)
        {
            double MapX = ScaleX * point.X + MapMinX;
            double MapY = ScaleY * (WinH - point.Y) + MapMinY;
            return new XVertex(MapX, MapY);
        }

        internal void UpdateMapWindow(Rectangle clientRectangle)
        {
            Update(CurrentMapExtent, clientRectangle);
        }

        internal void OffsetCenter(XVertex fromV, XVertex toV)
        {
            CurrentMapExtent.OffsetCenter(fromV, toV);
            Update(CurrentMapExtent, MapWindowSize);
        }

        internal double ToMapDistance(int pixelCount)
        {
            Point p1 = new Point(0, 0);
            Point p2 = new Point(0, pixelCount);
            XVertex v1 = ToMapVertex(p1);
            XVertex v2 = ToMapVertex(p2);
            return v1.Distance(v2);
        }
    }
    public class XExtent
    {
        public XVertex bottomLeft, upRight;


        double ZoomFactor = 2;
        double MovingFactor = 0.25;

        public XExtent(XVertex _oneCorner, XVertex _anotherCorner)
        {
            upRight = new XVertex(Math.Max(_anotherCorner.x, _oneCorner.x),
                                Math.Max(_anotherCorner.y, _oneCorner.y));
            bottomLeft = new XVertex(Math.Min(_anotherCorner.x, _oneCorner.x),
                                Math.Min(_anotherCorner.y, _oneCorner.y));
        }


        public XExtent(double x1, double x2, double y1, double y2)
        {
            double minX=Math.Min(x1,x2), 
                maxX=Math.Max(x1,x2), 
                minY=Math.Min(y1, y2), 
                maxY=Math.Max(y1,y2);
            bottomLeft =new XVertex(minX, minY);
            upRight=new XVertex(maxX, maxY);
        }

        /// <summary>
        /// copy extent from another one
        /// </summary>
        /// <param name="extent"></param>
        public XExtent(XExtent extent)
        {
            bottomLeft = new XVertex(extent.bottomLeft);
            upRight =new XVertex(extent.upRight);
        }

        public void ChangeExtent(XExploreActions action)
        {
            double newminx = bottomLeft.x, newminy = bottomLeft.y,
            newmaxx = upRight.x, newmaxy = upRight.y;
            switch (action)
            {
                case XExploreActions.zoomin:
                    newminx = ((GetMinX() + GetMaxX()) - GetWidth() / ZoomFactor) / 2;
                    newminy = ((GetMinY() + GetMaxY()) - GetHeight() / ZoomFactor) / 2;
                    newmaxx = ((GetMinX() + GetMaxX()) + GetWidth() / ZoomFactor) / 2;
                    newmaxy = ((GetMinY() + GetMaxY()) + GetHeight() / ZoomFactor) / 2;
                    break;
                case XExploreActions.zoomout:
                    newminx = ((GetMinX() + GetMaxX()) - GetWidth() * ZoomFactor) / 2;
                    newminy = ((GetMinY() + GetMaxY()) - GetHeight() * ZoomFactor) / 2;
                    newmaxx = ((GetMinX() + GetMaxX()) + GetWidth() * ZoomFactor) / 2;
                    newmaxy = ((GetMinY() + GetMaxY()) + GetHeight() * ZoomFactor) / 2;
                    break;
                case XExploreActions.moveup:
                    double offset = GetHeight() * MovingFactor;
                    newminy = GetMinY() -offset;
                    newmaxy = GetMaxY() -offset;
                    break;
                case XExploreActions.movedown:
                    newminy = GetMinY() + GetHeight() * MovingFactor;
                    newmaxy = GetMaxY() + GetHeight() * MovingFactor;
                    break;
                case XExploreActions.moveleft:
                    newminx = GetMinX() + GetWidth() * MovingFactor;
                    newmaxx = GetMaxX() + GetWidth() * MovingFactor;
                    break;
                case XExploreActions.moveright:
                    newminx = GetMinX() - GetWidth() * MovingFactor;
                    newmaxx = GetMaxX() - GetWidth() * MovingFactor;
                    break;
            }
            upRight.x = newmaxx;
            upRight.y = newmaxy;
            bottomLeft.x = newminx;
            bottomLeft.y = newminy;
        }

        internal double GetHeight()
        {
            return upRight.y - bottomLeft.y;
        }

        internal double GetMinX()
        {
            return bottomLeft.x;
        }

        internal double GetMinY()
        {
            return bottomLeft.y;
        }

        internal double GetMaxX()
        {
            return upRight.x;
        }

        internal double GetMaxY()
        {
            return upRight.y;
        }

        internal double GetWidth()
        {
            return upRight.x - bottomLeft.x;
        }

        internal bool IntersectOrNot(XExtent extent)
        {
            return !(
                GetMaxX() < extent.GetMinX() ||
                GetMinX() > extent.GetMaxX()||
                GetMaxY() < extent.GetMinY() || 
                GetMinY() > extent.GetMaxY()
                );
        }

        internal XVertex GetCenter()
        {
            return new XVertex(GetMinX()+GetWidth()/2, GetMinY()+GetHeight()/2);
        }

        internal void Merge(XExtent extent)
        {
            bottomLeft.x = Math.Min(bottomLeft.x, extent.bottomLeft.x);
            bottomLeft.y = Math.Min(bottomLeft.y, extent.bottomLeft.y);
            upRight.x = Math.Max(upRight.x, extent.upRight.x);
            upRight.y = Math.Max(upRight.y, extent.upRight.y);
        }

        internal void OffsetCenter(XVertex fromV, XVertex toV)
        {
            XVertex center = GetCenter();
            center.x -= toV.x - fromV.x;
            center.y -= toV.y - fromV.y;
            double width = GetWidth();
            double height = GetHeight();
            bottomLeft.x = center.x - width / 2;
            bottomLeft.y=center.y - height / 2;
            upRight.x=center.x + width / 2;
            upRight.y=center.y + height / 2;
        }

        internal bool Includes(XExtent extent)
        {
            return (
                GetMaxX() >= extent.GetMaxX() &&
                GetMinX() <= extent.GetMinX() &&
                GetMaxY() >= extent.GetMaxY() &&
                GetMinY() <= extent.GetMinY());
        }
    }

    public abstract class XSpatial
    {
        public XVertex centroid;
        public XExtent extent;

        public List<XVertex> vertexes;

        public XSpatial(List<XVertex> _vertexes)
        {
            //为节点数组赋值
            vertexes = _vertexes;

            //计算中心点centroid
            double x_cen = 0, y_cen = 0;
            foreach (XVertex v in _vertexes)
            {
                x_cen += v.x;
                y_cen += v.y;
            }
            x_cen /= _vertexes.Count;
            y_cen /= _vertexes.Count;
            centroid = new XVertex(x_cen, y_cen);

            //计算空间范围extent
            double x_min = double.MaxValue;
            double y_min = double.MaxValue;
            double x_max = double.MinValue;
            double y_max = double.MinValue;

            foreach (XVertex v in _vertexes)
            {
                x_min = Math.Min(x_min, v.x);
                y_min = Math.Min(y_min, v.y);
                x_max = Math.Max(x_max, v.x);
                y_max = Math.Max(y_max, v.y);
            }
            extent = new XExtent(new XVertex(x_min, y_min),
                new XVertex(x_max, y_max));
        }

        public abstract void draw(Graphics graphics, XView view, XThematic thematic);

        internal abstract double Distance(XVertex onevertex);
    }
    
    public class XAttribute
    {
        public ArrayList values;

        public XAttribute()
        {
            values = new ArrayList();
        }

        public XAttribute(XAttribute attribute)
        {
            values = new ArrayList();
            foreach (object v in attribute.values)
                values.Add(v);
        }

        public void Write(BinaryWriter bw)
        {
            for (int i = 0; i < values.Count; i++)
            {
                Type type = GetValue(i).GetType();
                if (type.ToString() == "System.Boolean")
                    bw.Write((bool)GetValue(i));
                else if (type.ToString() == "System.Byte")
                    bw.Write((byte)GetValue(i));
                else if (type.ToString() == "System.Char")
                    bw.Write((char)GetValue(i));
                else if (type.ToString() == "System.Decimal")
                    bw.Write((decimal)GetValue(i));
                else if (type.ToString() == "System.Double")
                    bw.Write((double)GetValue(i));
                else if (type.ToString() == "System.Single")
                    bw.Write((float)GetValue(i));
                else if (type.ToString() == "System.Int32")
                    bw.Write((int)GetValue(i));
                else if (type.ToString() == "System.Int64")
                    bw.Write((long)GetValue(i));
                else if (type.ToString() == "System.UInt16")
                    bw.Write((ushort)GetValue(i));
                else if (type.ToString() == "System.UInt32")
                    bw.Write((uint)GetValue(i));
                else if (type.ToString() == "System.UInt64")
                    bw.Write((ulong)GetValue(i));
                else if (type.ToString() == "System.SByte")
                    bw.Write((sbyte)GetValue(i));
                else if (type.ToString() == "System.Int16")
                    bw.Write((short)GetValue(i));
                else if (type.ToString() == "System.String")
                    XTools.WriteString((string)GetValue(i), bw);
            }
        }

        public XAttribute(List<XField> fs, BinaryReader br)
        {
            values = new ArrayList();
            for (int i = 0; i < fs.Count; i++)
            {
                Type type = fs[i].datatype;
                if (type.ToString() == "System.Boolean")
                    AddValue(br.ReadBoolean());
                else if (type.ToString() == "System.Byte")
                    AddValue(br.ReadByte());
                else if (type.ToString() == "System.Char")
                    AddValue(br.ReadChar());
                else if (type.ToString() == "System.Decimal")
                    AddValue(br.ReadDecimal());
                else if (type.ToString() == "System.Double")
                    AddValue(br.ReadDouble());
                else if (type.ToString() == "System.Single")
                    AddValue(br.ReadSingle());
                else if (type.ToString() == "System.Int32")
                    AddValue(br.ReadInt32());
                else if (type.ToString() == "System.Int64")
                    AddValue(br.ReadInt64());
                else if (type.ToString() == "System.UInt16")
                    AddValue(br.ReadUInt16());
                else if (type.ToString() == "System.UInt32")
                    AddValue(br.ReadUInt32());
                else if (type.ToString() == "System.UInt64")
                    AddValue(br.ReadUInt64());
                else if (type.ToString() == "System.SByte")
                    AddValue(br.ReadSByte());
                else if (type.ToString() == "System.Int16")
                    AddValue(br.ReadInt16());
                else if (type.ToString() == "System.String")
                    AddValue(XTools.ReadString(br));
            }
        }
        public void AddValue(object value)
        {
            values.Add(value);
        }

        public Object GetValue(int index)
        {
            return values[index];
        }

        public void draw(Graphics _Graphics, XView _view, XVertex _Location, int _Index)
        {
            Point point = _view.ToScreenPoint(_Location);

            _Graphics.DrawString(
                GetValue(_Index).ToString(), 
                new Font("宋体", 20),
                new SolidBrush(Color.Black), point);
        }
    }

    public class XFeature
    {
        public XSpatial spatial;
        public XAttribute attribute;

        public XFeature(XSpatial _spatial, XAttribute _attribute)
        {
            spatial = _spatial;
            attribute = _attribute;
        }

        public void draw(Graphics graphics, XView view, 
            bool DrawAttributeOrNot, int attributeIndex, XThematic thematic)
        {
            spatial.draw(graphics, view, thematic);
            if (DrawAttributeOrNot)
            {
                attribute.draw(graphics, view, spatial.centroid, attributeIndex);
            }
        }

        public object getAttribute(int index)
        {
            return attribute.GetValue(index);
        }

        internal double Distance(XVertex onevertex)
        {
            return spatial.Distance(onevertex);
        }
    }


    public class XVertex
    {
        public double x; 
        public double y;

        /// <summary>
        /// copy
        /// </summary>
        /// <param name="v"></param>
        public XVertex(XVertex v)
        {
            x= v.x;
            y= v.y;
        }

        public XVertex(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        internal double Distance(XVertex anothervertex)
        {
            return Math.Sqrt(
                (x - anothervertex.x) *
                (x - anothervertex.x) +
                (y - anothervertex.y) *
                (y - anothervertex.y)
                );
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(x);
            bw.Write(y);
        }

        internal bool IsSame(XVertex b)
        {
            return (Math.Abs(x-b.x)+Math.Abs(y-b.y))<0.0000001;
        }

        public XVertex(BinaryReader br)
        {
            x = br.ReadDouble();
            y = br.ReadDouble();
        }

    }

    public class XNode
    {
        public XVertex Location;
    }

    public class XPointSpatial: XSpatial
    {
        public XPointSpatial(XVertex location) : base(new List<XVertex> { location })
        {

        }

        public override void draw(Graphics graphics, XView view, XThematic thematic)
        {
            Point point = view.ToScreenPoint(centroid);
            int radius = thematic.PointRadius;
            graphics.FillEllipse(thematic.PointBrush,
                new Rectangle(
                    point.X - radius, point.Y - radius,
                radius*2, radius*2));
        }

        internal override double Distance(XVertex onevertex)
        {
            return centroid.Distance(onevertex);
        }
    }

    public class XLineSpatial: XSpatial
    {
        public double length;

        public XLineSpatial(List<XVertex> _vertexes) : base(_vertexes)
        {
            length = XTools.CalculateLength(_vertexes);
        }

        public override void draw(Graphics graphics, XView view, XThematic thematic)
        {
            Point[] points = view.ToScreenPoints(vertexes).ToArray();
            graphics.DrawLines(thematic.LinePen, points);

        }
        internal override double Distance(XVertex vertex)
        {
            double distance = Double.MaxValue;
            for (int i = 0; i < vertexes.Count - 1; i++)
            {
                double d = XTools.DistanceBetweenPointAndSegment(
                    vertexes[i], vertexes[i + 1], vertex);

                distance = Math.Min(d, distance);
            }
            return distance;
        }
    }

    public class XPolygonSpatial : XSpatial
    {
        double area;
        public XPolygonSpatial(List<XVertex> _vertexes) : base(_vertexes)
        {
            area=XTools.CalculateArea(_vertexes);
        }

        public override void draw(Graphics graphics, XView view, XThematic thematic)
        {
            Point[] points = view.ToScreenPoints(vertexes).ToArray();
            graphics.FillPolygon(thematic.PolygonBrush, points);
            graphics.DrawPolygon(thematic.PolygonPen, points);
        }

        internal override double Distance(XVertex vertex)
        {
            bool inside;
            if (Contains(vertex, out inside))
            {
                if (inside) return -1;
                else return 0;
            }
            else
            {
                List<XVertex> vs = new List<XVertex>();
                vs.AddRange(vertexes);
                vs.Add(vertexes[0]);
                XLineSpatial line = new XLineSpatial(vs);
                return line.Distance(vertex);
            }

        }

        /// <summary>
        /// 判断一个点和一个多边形的关系，如果多边形包括点，则返回true，否则false
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="inside">如果点在多边形的边线上，则为false，否则为true</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool Contains(XVertex vertex, out bool inside)
        {
            //交点的数量
            int count = 0;

            for (int i = 0; i < vertexes.Count; i++)
            {
                //满足情况3
                if (vertexes[i].IsSame(vertex))
                {
                    inside = false;
                    return true;
                }
                //由序号为i及next的两个节点构成一条线段，一般情况下next为i+1，
                //而针对最后一条线段，i为vertexes.Count-1，next为0
                int next = (i + 1) % vertexes.Count;
                //确定线段的坐标极值
                double minX = Math.Min(vertexes[i].x, vertexes[next].x);
                double minY = Math.Min(vertexes[i].y, vertexes[next].y);
                double maxX = Math.Max(vertexes[i].x, vertexes[next].x);
                double maxY = Math.Max(vertexes[i].y, vertexes[next].y);
                //如果线段是平行于射线的。
                if (minY == maxY)
                {
                    //满足情况2
                    if (minY == vertex.y && vertex.x >= minX && vertex.x <= maxX)
                    {
                        inside = false;
                        return true;
                    }
                    //满足情况1或者射线与线段平行无交点
                    else continue;
                }
                //点在线段坐标极值之外，不可能有交点
                if (vertex.x > maxX || vertex.y > maxY || vertex.y < minY)
                    continue;
                //计算交点横坐标，纵坐标无需计算，就是vertex.y
                double X0 = vertexes[i].x + (vertex.y - vertexes[i].y) *
                    (vertexes[next].x - vertexes[i].x) / (vertexes[next].y - vertexes[i].y);
                //交点在射线反方向，按无交点计算
                if (X0 < vertex.x) continue;
                //交点即为vertex，且在线段上
                if (X0 == vertex.x)
                {
                    inside = false;
                    return true;
                }
                //射线穿过线段下端点，不记数, 情况4
                if (vertex.y == minY) continue;
                //其他情况下，交点数加一
                count++;
            }
            //根据交点数量确定面是否包括点
            inside = true;
            return count % 2 != 0;
        }
    }
}
