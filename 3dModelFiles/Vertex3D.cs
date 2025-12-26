using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ThreeDModelFiles
{
    public class Vertex3D
    {
        public int Index { get; set; }
        public Point3D Location { get; set; }

        public Vertex3D(double x, double y, double z)
            : this(new Point3D(x, y, z))
        { }

        public Vertex3D(Point3D location)
        {
            Index = -1;
            Location = location;
        }

        public static implicit operator Point3D(Vertex3D vertex3D)
        {
            return vertex3D.Location;
        }
    }
}
