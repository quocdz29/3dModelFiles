using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreeDModelFiles;

namespace Test3dModelFiles
{
    [TestClass]
    public sealed class Test3MF
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataRow(data: "TestFiles\\Pentahedron.3mf")]
        public void TestLoad(string filename)
        {
            //string filename = "Pentahedron.3mf";
            FileInfo file = new FileInfo(filename);

            Assert.IsTrue(file.Exists);

            List<CommonFileData> results = _3MFFile.Load(file);

            foreach (CommonFileData data in results)
            {
                bool maxIndexExceedsVertexCount = (data.TriangleIndices.Max() > (data.Positions.Count-1));
                Assert.IsFalse(maxIndexExceedsVertexCount, $"Largest {nameof(CommonFileData)}.{nameof(CommonFileData.TriangleIndices)} value exceeds the quantity of available vertices in {nameof(CommonFileData)}.{nameof(CommonFileData.Positions)}. Every entry in {nameof(CommonFileData.TriangleIndices)} must be a valid index into the {nameof(CommonFileData)}.{nameof(CommonFileData.Positions)} collection. ");

                bool isDivisibleBy3 = ((data.TriangleIndices.Count  % 3) == 0);
                Assert.IsTrue(isDivisibleBy3, $"{nameof(CommonFileData)}.{nameof(CommonFileData.TriangleIndices)}.{nameof(Int32Collection.Count)} is not divisible by 3. This is a problem because every group of three indices positions becomes a triangle.");

                TestContext.WriteLine($"{nameof(CommonFileData)}.{nameof(CommonFileData.ID)}: {data.ID}");
                TestContext.WriteLine($"{nameof(CommonFileData)}.{nameof(CommonFileData.Positions)}.{nameof(CommonFileData.Positions.Count)}: {data.Positions.Count}");
                TestContext.WriteLine($"{nameof(CommonFileData)}.{nameof(CommonFileData.TriangleIndices)}.{nameof(CommonFileData.TriangleIndices.Count)}: {data.TriangleIndices.Count}");
                TestContext.WriteLine($"{nameof(CommonFileData)}.{nameof(CommonFileData.Normals)}.{nameof(CommonFileData.Normals.Count)}: {data.Positions.Count}");

                if (data.Transform != Matrix3D.Identity)
                {
                    TestContext.WriteLine($"{nameof(CommonFileData)}.{nameof(CommonFileData.Transform)}: {data.Transform.ToString()}");
                }
                TestContext.WriteLine("");

                TestContext.WriteLine("---");
                TestContext.WriteLine("");
            }
        }

        [TestMethod]
        public void TestSave()
        {
            FileInfo originalFileInfo = new FileInfo("TestFiles\\Pentahedron.3mf");
            FileInfo reproducedFileInfo = new FileInfo( Path.GetFullPath("Test_3DModel.3mf"));

            var original_FileDataCollection = _3MFFile.Load(originalFileInfo);

            bool result = _3MFFile.Save(original_FileDataCollection, reproducedFileInfo);

            string fileData = string.Empty;
            if (reproducedFileInfo.Exists && reproducedFileInfo.Length > 0)
            {
                fileData = File.ReadAllText(reproducedFileInfo.FullName);
            }

            Assert.IsTrue(result, "_3MFFile.Save result.");
            //Assert.IsTrue(reproducedFileInfo.Exists, "reproducedFileInfo.Exists");
            //Assert.IsTrue(reproducedFileInfo.Length > 0, "reproducedFileInfo.Length > 0");

            List<CommonFileData> loadedList = _3MFFile.Load(reproducedFileInfo);

            CommonFileData reproduced_FileData = loadedList.FirstOrDefault();

            CommonFileData  original_FileData = original_FileDataCollection.FirstOrDefault(); ;

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

            TestContext.WriteLine("");
        }
    }
}
