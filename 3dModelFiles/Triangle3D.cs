using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ThreeDModelFiles
{
    public class Triangle3D
    {
        public Vector3D Normal { get; set; }
        public Vertex3D A { get; set; }
        public Vertex3D B { get; set; }
        public Vertex3D C { get; set; }

        public Triangle3D(Vertex3D a, Vertex3D b, Vertex3D c)
        {
            Normal = default(Vector3D);
            A = a;
            B = b;
            C = c;
        }

        public Triangle3D(Vector3D normal, Vertex3D a, Vertex3D b, Vertex3D c)
            : this(a, b, c)
        {
            Normal = normal;
        }
    }
}
