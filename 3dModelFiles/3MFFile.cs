using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;
using System.Xml.Linq;

using static ThreeDModelFiles.STLFile;

namespace ThreeDModelFiles
{

    public static partial class _3MFFile
    {
        private static string XPath_Material = "/model/resources/basematerials/base/@displaycolor";
        private static string XPath_Vertex = "/model/resources/object/mesh/vertices/vertex";
        private static string XPath_Triangle = "/model/resources/object/mesh/triangles/triangle";
        private static string XPath_Transform = "/model/build/item/@transform";
        private static string XPath_TransformId = "/model/build/item/@objectid";

        public static List<CommonFileData> Load(FileInfo source)
        {
            if (!source.Exists)
            {
                throw new FileNotFoundException("File not found.", source.FullName);
            }
            if (source.Length < 1)
            {
                throw new FileFormatException("File is empty.");
            }

            string modelFile = _3MFArchive.ExtractModelFile(source);

            List<CommonFileData> results =     Read.ModelFile(modelFile).ToList();
            return results;
        }

        public static bool Save(List<CommonFileData> data, FileInfo destination)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            List<int> ids = data.Select(cfd => cfd.ID).ToList();
            List<int> uniqueIds = ids.Distinct().ToList();
            if (ids.Count != uniqueIds.Count)
            {
                AutoCounter idCounter = new AutoCounter();
                foreach (var fileData in data)
                {
                    fileData.ID = idCounter.GetNext();
                }
            }


            string modelFile = Write.Model(data);


            return _3MFArchive.PackModelFile(modelFile, destination);
        }

        private static class Write
        {
            private static string FileHeader = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<model xmlns=\"http://schemas.microsoft.com/3dmanufacturing/core/2015/02\" unit=\"millimeter\" xml:lang=\"en-US\" xmlns:m=\"http://schemas.microsoft.com/3dmanufacturing/material/2015/02\">";

            public static string Model(List<CommonFileData> datas)
            {
                List<Tuple<int,Color>> materials = new List<Tuple<int, Color>>();
                List<string> objectBlocks = new List<string>();
                List<Tuple<int,Matrix3D>> transforms = new List<Tuple<int, Matrix3D>>();

                foreach (CommonFileData data in datas)
                {
                    materials.Add(new Tuple<int, Color>(data.ID, data.MaterialColor));
                    transforms.Add(new Tuple<int, Matrix3D>(data.ID, data.Transform));
                }

                StringBuilder result = new StringBuilder();
                result.AppendLine(FileHeader);
                result.AppendLine("\t<resources>");
                result.AppendLine(Materials(1, materials));

                foreach (CommonFileData data in datas)
                {

                    result.AppendLine("\t\t" + $"<object id=\"{data.ID}\" type=\"model\">");
                    result.AppendLine("\t\t\t<mesh>");

                    result.AppendLine("\t\t\t\t<vertices>");
                    result.AppendLine("\t\t\t\t\t" + string.Join(Environment.NewLine + "\t\t\t\t\t", data.Positions.Select(p => Write.Point3d(p)).ToArray()));
                    result.AppendLine("\t\t\t\t</vertices>");

                    result.AppendLine("\t\t\t\t<triangles>");
                    result.AppendLine("\t\t\t\t\t" + string.Join(Environment.NewLine + "\t\t\t\t\t", data.TriangleIndices.Chunk(3).Select(chnk => Write.TriangleIndices3(chnk)).ToArray()));
                    result.AppendLine("\t\t\t\t</triangles>");

                    result.AppendLine("\t\t\t</mesh>");
                    result.AppendLine("\t\t</object>");

                }

                result.AppendLine("\t</resources>");
                result.AppendLine(Transforms(transforms));
                result.AppendLine("</model>");

                return result.ToString();
            }


            private static string Transforms(List<Tuple<int, Matrix3D>> transforms)
            {
                return
                    "\t<build>\r\n" +
                    string.Join(Environment.NewLine, transforms.Select(tup => Transform(tup.Item2, tup.Item1))) +
                     "\t</build>";
            }

            private static string Transform(Matrix3D m, int id)
            {
                return "\t\t" + $"<item objectid=\"{id}\" transform=\"" +
                    $"{m.M11} {m.M12} {m.M13} {m.M14} " +
                    $"{m.M21} {m.M22} {m.M23} {m.M24} " +
                    $"{m.M31} {m.M32} {m.M33} {m.M34} " +
                    $"{m.OffsetX} {m.OffsetY} {m.OffsetZ} {m.M44}\" />{Environment.NewLine}";
            }

            private static string Materials(int id, List<Tuple<int, Color>> materials)
            {
                return "\t\t" + $"<basematerials id=\"{id}\">" + Environment.NewLine +
                     string.Join(Environment.NewLine, materials.Select(tup => $"<base name=\"Material\" displaycolor=\"{HexColor(tup.Item2)}\" />")) + Environment.NewLine +
                      "\t\t</basematerials>";
            }

            private static string TriangleIndices3(int[] triangleIndices)
            {
                return $"<triangle v1=\"{triangleIndices[0]}\" v2=\"{triangleIndices[1]}\" v3=\"{triangleIndices[2]}\" pid=\"1\" p1=\"0\" />";
            }

            private static string Point3d(Point3D point3D)
            {
                return $"<vertex x=\"{Math.Round(point3D.X, 6)}\" y=\"{Math.Round(point3D.Y, 6)}\" z=\"{Math.Round(point3D.Z, 6)}\" />";
            }

            private static string HexColor(Color color)
            {
                return $"#{color.R.ToString("X2")}{color.G.ToString("X2")}{color.B.ToString("X2")}".ToUpper();
            }

            private static string WrapIn(string body, string elementName, int indent)
            {
                string indentString = new string('\t', indent);
                return $"{indentString}<{elementName}>" + Environment.NewLine +
                    indentString + "\t" +
                    body + Environment.NewLine +
                    $"{indentString}</{elementName}>";
            }
        }

        private static class Read
        {
            private static string _3MF_Namespace = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";

            public static IEnumerable<CommonFileData> ModelFile(string fileContents)
            {
                XDocument xDocument = XDocument.Parse(fileContents);

                // 3MF default namespace
                XNamespace ns = _3MF_Namespace;

                var modelTransformDictionary = new Dictionary<int, Matrix3D>();

                IEnumerable<XElement> items = xDocument.Descendants(ns + "build").Descendants(ns + "item");
                foreach (XElement item in items)
                {
                    var id = item.Attribute("objectid").Value;
                    if (!int.TryParse(id, out int objectId))
                    {
                        throw new Exception($"Could not find or parse the expected attribute {XPath_TransformId} to link the transform up with the model object.");
                    }

                    // Parse build item transform (first <build>/<item> element)
                    string tAttr = (string?)item.Attribute("transform") ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(tAttr))
                    {
                        continue;
                    }

                    // Split on whitespace
                    string[] values = tAttr.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length != 12)
                    {
                        continue;
                    }

                    double[] vals = new double[12];
                    for (int i = 0; i < 12; i++)
                    {
                        if (!double.TryParse(values[i], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out vals[i]))
                        {
                            throw new Exception($"A value in the attribute {XPath_Transform} failed to parse into a System.Double.");
                        }
                    }

                    // Map 12 values to a 4x4 Matrix3D:
                    // [ a b c 0
                    //   d e f 0
                    //   g h i 0
                    //   j k l 1 ]
                    Matrix3D transform = new Matrix3D(
                     vals[0], vals[1], vals[2], 0,
                     vals[3], vals[4], vals[5], 0,
                     vals[6], vals[7], vals[8], 0,
                     vals[9], vals[10], vals[11], 1);

                    modelTransformDictionary.Add(objectId, transform);
                }

                IEnumerable<XElement> objectNodes = xDocument.Descendants(ns + "object");
                foreach (XElement obj in objectNodes)
                {
                    CommonFileData result = new CommonFileData();

                    var id = obj.Attribute("id").Value;
                    if (int.TryParse(id, out int modelId))
                    {
                        result.ID = modelId;

                        if (modelTransformDictionary.ContainsKey(modelId))
                        {
                            result.Transform = modelTransformDictionary[modelId];
                        }
                    }

                    // Parse vertices into Positions
                    IEnumerable<XElement> vertexElements = xDocument.Descendants(ns + "vertex");
                    foreach (XElement v in vertexElements)
                    {
                        string sx = (string?)v.Attribute("x") ?? "0";
                        string sy = (string?)v.Attribute("y") ?? "0";
                        string sz = (string?)v.Attribute("z") ?? "0";

                        if (!double.TryParse(sx, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double x))
                        {
                            throw new Exception($@"The element at the XPath ""{XPath_Vertex}"" could not parsed into a System.Double.");
                        }
                        if (!double.TryParse(sy, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double y))
                        {
                            throw new Exception($@"The element at the XPath ""{XPath_Vertex}"" could not parsed into a System.Double.");
                        }
                        if (!double.TryParse(sz, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double z))
                        {
                            throw new Exception($@"The element at the XPath ""{XPath_Vertex}"" could not parsed into a System.Double.");
                        }

                        result.Positions.Add(new Point3D(x, y, z));
                    }

                    // Parse triangles into TriangleIndices (v1, v2, v3 in order)
                    Int32Collection indices = new Int32Collection();
                    IEnumerable<XElement> triangleElements = xDocument.Descendants(ns + "triangle");
                    foreach (XElement t in triangleElements)
                    {
                        string sv1 = (string?)t.Attribute("v1") ?? "0";
                        string sv2 = (string?)t.Attribute("v2") ?? "0";
                        string sv3 = (string?)t.Attribute("v3") ?? "0";

                        if (!int.TryParse(sv1, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v1))
                        {
                            throw new Exception($@"The element at the XPath ""{XPath_Triangle}"" could not parsed into a System.Int32.");
                        }
                        if (!int.TryParse(sv2, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v2))
                        {
                            throw new Exception($@"The element at the XPath ""{XPath_Triangle}"" could not parsed into a System.Int32.");
                        }
                        if (!int.TryParse(sv3, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v3))
                        {
                            throw new Exception($@"The element at the XPath ""{XPath_Triangle}"" could not parsed into a System.Int32.");
                        }

                        result.TriangleIndices.Add(v1);
                        result.TriangleIndices.Add(v2);
                        result.TriangleIndices.Add(v3);
                    }
                    yield return result;
                }

                yield break;
            }
        }
    }
}
