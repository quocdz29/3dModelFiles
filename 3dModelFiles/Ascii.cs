using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ThreeDModelFiles
{
    public partial class STLFile
    {
        public static class Ascii
        {
            private static string StlFile_StartToken = "solid ";
            private static string FacetBlock_StartToken = "facet ";
            private static string FacetBlock_EndToken = "endfacet";
            private static string FormatExample_Vertex = "Expected format: \"vertex 4.004000 2.000000 0.000001\"";
            private static string FormatExample_Normal = "Expected format: \"facet normal 4.004000 2.000000 0.000001\"";

            public static CommonFileData Load(FileInfo stlFile)
            {
                if (!stlFile.Exists)
                {
                    throw new FileNotFoundException("File not found.", stlFile.FullName);
                }
                if (stlFile.Length < 1)
                {
                    throw new FileFormatException("File is empty.");
                }

                string stlFileContents = File.ReadAllText(stlFile.FullName);

                if (!stlFileContents.StartsWith(StlFile_StartToken))
                {
                    throw new Exception($"Expected file to begin with line: \"{StlFile_StartToken}\"");
                }

                CommonFileData results = new CommonFileData();

                string facet = Read.ExtractFacet(ref stlFileContents);
                while (facet != null)
                {
                    Triangle3D triangle = Read.ParseFacet(facet);
                    results.FromTriangle3D(triangle);

                    facet = Read.ExtractFacet(ref stlFileContents);
                }

                return results;
            }

            public static bool Save(CommonFileData data,  FileInfo stlFile)
            {
                if (stlFile.Exists)
                {
                    stlFile.Delete();
                }

                List<Triangle3D> triangleCollection = data.ToTriangle3DCollection();

                string fileData = Write.Mesh(triangleCollection);                
                File.WriteAllText(stlFile.FullName, fileData);

                return true;
            }

            private static class Read
            {
                public static string ExtractFacet(ref string stlFileContents)
                {
                    int startIndex = stlFileContents.IndexOf(FacetBlock_StartToken);
                    int endIndex = stlFileContents.IndexOf(FacetBlock_EndToken) + FacetBlock_EndToken.Length;

                    if (startIndex == -1 || endIndex == -1)
                    {
                        return null;
                    }

                    int length = endIndex - startIndex;

                    string result = stlFileContents.Substring(startIndex, length);

                    stlFileContents = stlFileContents.Substring(endIndex);

                    return result;
                }

                public static Triangle3D ParseFacet(string stlFacetBlock)
                {
                    string[] lines = stlFacetBlock.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length != 7)
                    {
                        throw new Exception("Expected 7 lines: facet normal X Y Z, outer loop, vertex  X Y Z, vertex  X Y Z, vertex  X Y Z, endloop, endfacet");
                    }
                    return ParseTriangle(lines[0], lines[2], lines[3], lines[4]);
                }

                private static Triangle3D ParseTriangle(string normalLine, string vertexLineA, string vertexLineB, string vertexLineC)
                {
                    string[] normalParts = normalLine.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (normalParts.Length != 5)
                    {
                        throw new FormatException("Expected 5 parts delimited by spaces.\n" + FormatExample_Normal);
                    }

                    if (!string.Equals(normalParts[0], "facet", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(normalParts[1], "normal", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new FormatException(FormatExample_Normal);
                    }

                    if (!double.TryParse(normalParts[2], out double nx) ||
                        !double.TryParse(normalParts[3], out double ny) ||
                        !double.TryParse(normalParts[4], out double nz))
                    {
                        throw new FormatException("Normal vector components could not be parsed into System.Double values.\n" + FormatExample_Normal);
                    }

                    Vector3D normal = new Vector3D(nx, ny, nz);
                    Vertex3D a = ParseVertex(vertexLineA);
                    Vertex3D b = ParseVertex(vertexLineB);
                    Vertex3D c = ParseVertex(vertexLineC);

                    return new Triangle3D(normal, a, b, c);
                }

                private static Vertex3D ParseVertex(string stlVertexLine)
                {
                    string[] parts = stlVertexLine.Trim().Split(new char[] { ' ' });
                    if (parts.Length != 4)
                    {
                        throw new Exception("Expected 4 parts.\n" + FormatExample_Vertex);
                    }

                    if (!string.Equals(parts[0], "vertex", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception(FormatExample_Vertex);
                    }

                    if (!double.TryParse(parts[1], out double x))
                    {
                        throw new Exception($"\"{parts[1]}\" could be parsed into a System.Double. " + FormatExample_Vertex);
                    }
                    if (!double.TryParse(parts[2], out double y))
                    {
                        throw new Exception($"\"{parts[2]}\" could be parsed into a System.Double. " + FormatExample_Vertex);
                    }
                    if (!double.TryParse(parts[3], out double z))
                    {
                        throw new Exception($"\"{parts[3]}\" could be parsed into a System.Double. " + FormatExample_Vertex);
                    }

                    return new Vertex3D(new Point3D(x, y, z));
                }
            }

            private static class Write
            {
                public static string Mesh(List<Triangle3D> triangleCollection)
                {
                    string headerString = SaveHelper.GetFileHeaderString();

                    return $"solid {headerString}" + Environment.NewLine +
                            string.Join(Environment.NewLine, triangleCollection.Select(triangle => Write.Triangle3D(triangle))) + Environment.NewLine +
                            $"endsolid {headerString}" + Environment.NewLine;
                }

                public static string Triangle3D(Triangle3D triangle3d)
                {
                    string[] lines = new string[]
                    {
                        $"facet normal {Math.Round(triangle3d.Normal.X, 6)} {Math.Round(triangle3d.Normal.Y, 6)} {Math.Round(triangle3d.Normal.Z, 6)}",
                        "outer loop" ,
                        Write.Vertex3D(triangle3d.A),
                        Write.Vertex3D(triangle3d.B),
                        Write.Vertex3D(triangle3d.C),
                        "endloop" ,
                        "endfacet"
                    };

                    return string.Join(Environment.NewLine, lines);
                }

                private static string Vertex3D(Vertex3D vertex3d)
                {
                    return $"vertex {Math.Round(vertex3d.Location.X, 6)} {Math.Round(vertex3d.Location.Y, 6)} {Math.Round(vertex3d.Location.Z, 6)}";
                }
            }
        }
    }
}
