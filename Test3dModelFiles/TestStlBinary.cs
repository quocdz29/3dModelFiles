using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreeDModelFiles;

namespace Test3dModelFiles
{
    [TestClass]
    public sealed class TestStlBinary
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataRow(data: "TestFiles\\Tetrahedron-Binary.stl")]
        public void TestLoad(string filename)
        {
            FileInfo file = new FileInfo(filename);

            Assert.IsTrue(file.Exists);

            CommonFileData results = STLFile.Binary.Load(file);

            bool maxIndexExceedsVertexCount = (results.TriangleIndices.Max() > (results.Positions.Count-1));
            Assert.IsFalse(maxIndexExceedsVertexCount, $"Largest {nameof(CommonFileData)}.{nameof(CommonFileData.TriangleIndices)} value exceeds the quantity of available vertices in {nameof(CommonFileData)}.{nameof(CommonFileData.Positions)}. Every entry in {nameof(CommonFileData.TriangleIndices)} must be a valid index into the {nameof(CommonFileData)}.{nameof(CommonFileData.Positions)} collection. ");

            bool isDivisibleBy3 = ((results.TriangleIndices.Count  % 3) == 0);
            Assert.IsTrue(isDivisibleBy3, $"{nameof(CommonFileData)}.{nameof(CommonFileData.TriangleIndices)}.{nameof(Int32Collection.Count)} is not divisible by 3. This is a problem because every group of three indices positions becomes a triangle.");

            TestContext.WriteLine($"{nameof(CommonFileData)}.{nameof(CommonFileData.Positions)}.{nameof(CommonFileData.Positions.Count)}: {results.Positions.Count}");
            TestContext.WriteLine($"{nameof(CommonFileData)}.{nameof(CommonFileData.TriangleIndices)}.{nameof(CommonFileData.TriangleIndices.Count)}: {results.TriangleIndices.Count}");
            TestContext.WriteLine($"{nameof(CommonFileData)}.{nameof(CommonFileData.Normals)}.{nameof(CommonFileData.Normals.Count)}: {results.Positions.Count}");

            if (results.Transform != Matrix3D.Identity)
            {
                TestContext.WriteLine($"{nameof(CommonFileData)}.{nameof(CommonFileData.Transform)}: {results.Transform.ToString()}");
            }
        }

        [TestMethod]
        public void TestSave()
        {
            FileInfo originalFileInfo = new FileInfo( "TestFiles\\Tetrahedron-Binary.stl");
            FileInfo reproducedFileInfo = new FileInfo( Path.GetFullPath("Test_Binary.stl"));

            CommonFileData original_FileData = STLFile.Binary.Load(originalFileInfo);
          
            bool result = STLFile.Binary.Save(original_FileData, reproducedFileInfo.FullName);

            Assert.IsTrue(result, "STLFile.Binary.Save result.");
            Assert.IsTrue(reproducedFileInfo.Exists, "reproducedFileInfo.Exists");
            Assert.IsTrue(reproducedFileInfo.Length > 0, "reproducedFileInfo.Length > 0");

            CommonFileData reproduced_FileData = STLFile.Binary.Load(reproducedFileInfo);

            TestContext.WriteLine($"  Original.Positions.Count: {original_FileData.Positions.Count}");
            TestContext.WriteLine($"Reproduced.Positions.Count: {reproduced_FileData.Positions.Count}");
            TestContext.WriteLine("");
            TestContext.WriteLine($"  Original.TriangleIndices.Count: {original_FileData.TriangleIndices.Count}");
            TestContext.WriteLine($"Reproduced.TriangleIndices.Count: {reproduced_FileData.TriangleIndices.Count}");
            TestContext.WriteLine("");
            TestContext.WriteLine($"  Original.Normals.Count: {original_FileData.Normals.Count}");
            TestContext.WriteLine($"Reproduced.Normals.Count: {reproduced_FileData.Normals.Count}");
            TestContext.WriteLine("");
            TestContext.WriteLine($"  Original.File.Length: {originalFileInfo.Length}");
            TestContext.WriteLine($"Reproduced.File.Length: {reproducedFileInfo.Length}");
            TestContext.WriteLine("");
            TestContext.WriteLine($"Output file path: {reproducedFileInfo.FullName}");
            TestContext.WriteLine("");

            Assert.AreEqual(original_FileData.Positions.Count, reproduced_FileData.Positions.Count, "Positions.Count");
            Assert.AreEqual(original_FileData.TriangleIndices.Count, reproduced_FileData.TriangleIndices.Count, "TriangleIndices.Count");
            Assert.AreEqual(original_FileData.Normals.Count, reproduced_FileData.Normals.Count, "Normals.Count");
        }
    }
}
