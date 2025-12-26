using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml.Linq;

namespace ThreeDModelFiles
{
    public class CommonFileData
    {
        public int ID { get; set; }
        public Point3DCollection Positions { get; set; }
        public Vector3DCollection Normals { get; set; }
        public Int32Collection TriangleIndices { get; set; }
        public PointCollection TextureCoordinates { get; set; }
        public Matrix3D Transform { get; set; } = Matrix3D.Identity;
        public Color MaterialColor { get; set; }
        public string MaterialFilePath { get; set; } = string.Empty;

        public CommonFileData()
        {
            ID = -1;
            Positions = new Point3DCollection();
            Normals = new Vector3DCollection();
            TriangleIndices = new Int32Collection();
            TextureCoordinates = new PointCollection();
            Transform = Matrix3D.Identity;
            MaterialColor = Colors.DarkSlateGray;
        }

        public void FromTriangle3D(Triangle3D triangle)
        {
            Positions.Add(triangle.A.Location);
            triangle.A.Index = Positions.Count - 1;

            Positions.Add(triangle.B.Location);
            triangle.B.Index = Positions.Count - 1;

            Positions.Add(triangle.C.Location);
            triangle.C.Index = Positions.Count - 1;

            TriangleIndices.Add(triangle.A.Index);
            TriangleIndices.Add(triangle.B.Index);
            TriangleIndices.Add(triangle.C.Index);

            if (triangle.Normal != default(Vector3D))
            {
                Normals.Add(triangle.Normal);
            }
        }

        public string ToXamlString()
        {
            GeometryModel3D model = ToGeometryModel3D();
            return System.Windows.Markup.XamlWriter.Save(model);
        }

        public MeshGeometry3D ToMeshGeometry3D()
        {
            var result = new MeshGeometry3D()
            {
                Positions = Positions,
                TriangleIndices = TriangleIndices
            };

            if (TextureCoordinates.Any())
            {
                result.TextureCoordinates = TextureCoordinates;
            }

            if (Normals.Any())
            {
                result.Normals = Normals;
            }

            return result;
        }

        public GeometryModel3D ToGeometryModel3D()
        {
            return new GeometryModel3D()
            {
                Geometry = ToMeshGeometry3D(),
                Transform = new MatrixTransform3D(Transform)
            };
        }

        public List<Triangle3D> ToTriangle3DCollection()
        {
            Vertex3D[] vertices = Positions.Select(pnt3d => new Vertex3D(pnt3d)).ToArray();

            var triangleIndexTriples = TriangleIndices.Chunk(3).ToList();
            var results = triangleIndexTriples
                    .Select(chk => new Triangle3D(vertices[chk[0]], vertices[chk[1]], vertices[chk[2]]))
                    .ToList();

            if (results.Count == Normals.Count)
            {
                int index = 0;
                int max = results.Count;
                while (index < max)
                {
                    results[index].Normal = Normals[index];
                    index++;
                }
            }

            return results;
        }

    }
}
