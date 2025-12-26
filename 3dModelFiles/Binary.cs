using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ThreeDModelFiles
{
    public partial class STLFile
    {
        public static class Binary
        {
            public static CommonFileData Load(FileInfo stlFile)
            {
                if (!stlFile.Exists)
                {
                    throw new FileNotFoundException("File not found.", stlFile.FullName);
                }

                AutoCounter indexCounter = new AutoCounter();
                CommonFileData results = new CommonFileData();

                using (FileStream fileStream = stlFile.OpenRead())
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        byte[] header = binaryReader.ReadBytes(80);
                        UInt32 triangleCount = binaryReader.ReadUInt32();

                        if (triangleCount == 0)
                        {
                            return results;
                        }

                        for (UInt32 i = 0; i < triangleCount; ++i)
                        {
                            Triangle3D triangle = Read.Triangle3D(binaryReader);
                            byte[] attribute = binaryReader.ReadBytes(2); // Not used

                            results.FromTriangle3D(triangle);
                        }
                    }
                }

                return results;
            }


            public static bool Save(CommonFileData data, string filename)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }
                FileInfo stlFile = new FileInfo(filename);
                if (stlFile.Exists)
                {
                    stlFile.Delete();
                }

                List<Triangle3D> triangleCollection = data.ToTriangle3DCollection();
                UInt32 triangleCount = (UInt32)triangleCollection.Count;

                if (triangleCount < 1)
                {
                    return false;
                }

                using (FileStream fileStream = File.Create(filename))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                    {
                        byte[] header = Write.GetBinaryHeader();

                        binaryWriter.Write(header);
                        binaryWriter.Write(triangleCount);

                        byte[] attributeData = new byte[] { 0, 0 };

                        foreach (Triangle3D triangle3D in triangleCollection)
                        {
                            Write.Triangle3D(binaryWriter, triangle3D);
                            binaryWriter.Write(attributeData);
                        }
                    }
                }

                return true;
            }

            private static class Read
            {
                public static Triangle3D Triangle3D(BinaryReader binaryReader)
                {
                    return new Triangle3D
                    (
                        Read.Vector3D(binaryReader),
                        Read.Vertex3D(binaryReader),
                        Read.Vertex3D(binaryReader),
                        Read.Vertex3D(binaryReader)
                    );
                }

                private static Vertex3D Vertex3D(BinaryReader binaryReader)
                {
                    return new Vertex3D
                    (
                        binaryReader.ReadSingle(),
                        binaryReader.ReadSingle(),
                        binaryReader.ReadSingle()
                    );
                }

                private static Vector3D Vector3D(BinaryReader binaryReader)
                {
                    return new Vector3D()
                    {
                        X = binaryReader.ReadSingle(),
                        Y = binaryReader.ReadSingle(),
                        Z = binaryReader.ReadSingle()
                    };
                }
            }

            private static class Write
            {
                private static byte[] _binaryHeaderBytes = null;

                public static byte[] GetBinaryHeader()
                {
                    if (_binaryHeaderBytes == null)
                    {
                        string headerString = SaveHelper.GetFileHeaderString();
                        byte[] result = UTF8Encoding.UTF8.GetBytes(headerString);

                        if (result.Length > 80)
                        {
                            result = result.Take(80).ToArray();
                        }
                        else if (result.Length < 80)
                        {
                            int paddingBytesRequired = 80 - result.Length;
                            byte[] nullPadding = Enumerable.Repeat((byte)0, paddingBytesRequired).ToArray();
                            result = result.Concat(nullPadding).ToArray();
                        }

                        _binaryHeaderBytes = result;
                    }

                    return _binaryHeaderBytes;
                }

                public static void Triangle3D(BinaryWriter binaryWriter, Triangle3D triangle3D)
                {
                    Write.Vector3D(binaryWriter, triangle3D.Normal);
                    Write.Vertex3D(binaryWriter, triangle3D.A);
                    Write.Vertex3D(binaryWriter, triangle3D.B);
                    Write.Vertex3D(binaryWriter, triangle3D.C);
                }

                private static void Vertex3D(BinaryWriter binaryWriter, Vertex3D vertex3D)
                {
                    binaryWriter.Write((Single)vertex3D.Location.X);
                    binaryWriter.Write((Single)vertex3D.Location.Y);
                    binaryWriter.Write((Single)vertex3D.Location.Z);

                }

                private static void Vector3D(BinaryWriter binaryWriter, Vector3D vector3D)
                {
                    binaryWriter.Write((Single)vector3D.X);
                    binaryWriter.Write((Single)vector3D.Y);
                    binaryWriter.Write((Single)vector3D.Z);
                }
            }
        }
    }
}
