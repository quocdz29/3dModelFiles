using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeDModelFiles
{
    public static class _3MFArchive
    {
        private static string ModelFile_ArchiveEntryName = "3D/3dmodel.model";
        private static string ContentTypesFile_ArchiveEntryName = "[Content_Types].xml";
        private static string RelsFile_ArchiveEntryName = "_rels/.rels";
        private static string RelsFile = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\r\n<Relationship Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\" Target=\"/3D/3dmodel.model\" Id=\"rel0\" />\r\n</Relationships>";
        private static string ContentTypesFile = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\r\n\t<Default Extension=\"jpeg\" ContentType=\"image/jpeg\" />\r\n\t<Default Extension=\"jpg\" ContentType=\"image/jpeg\" />\r\n\t<Default Extension=\"model\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodel+xml\" />\r\n\t<Default Extension=\"png\" ContentType=\"image/png\" />\r\n\t<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />\r\n\t<Default Extension=\"texture\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodeltexture\" />\r\n</Types>";

        /// <summary>
        /// Extracts the 3D/3dmodel.model file from the 3MF archive format
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileFormatException"></exception>
        /// <exception cref="FormatException"></exception>
        public static string ExtractModelFile(FileInfo source)
        {
            if (!source.Exists)
            {
                throw new FileNotFoundException("File not found.", source.FullName);
            }
            if (source.Length < 1)
            {
                throw new FileFormatException("File is empty.");
            }

            string result = string.Empty;

            using (Stream stream = source.OpenRead())
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
                {
                    ReadOnlyCollection<ZipArchiveEntry> entries = archive.Entries;

                    ZipArchiveEntry modelFile = entries.Where(zarc => string.Equals(zarc.FullName, ModelFile_ArchiveEntryName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (modelFile == null)
                    {
                        throw new FormatException($"Expected .3MF file to contain {nameof(ZipArchiveEntry)} named {ModelFile_ArchiveEntryName}");
                    }

                    using (StreamReader archiveFileStream = new StreamReader(modelFile.Open(), true))
                    {
                        result = archiveFileStream.ReadToEnd();
                        //results.AddRange(Read.ModelFile(archiveFileStream));
                    }
                }
            }

            return result;
        }

        public static bool PackModelFile(string modelFile, FileInfo destination)
        {
            using (Stream stream = destination.Create())
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, false))
                {
                    ZipArchiveEntry relsEntry = archive.CreateEntry(RelsFile_ArchiveEntryName);
                    using (StreamWriter relsFileStream = new StreamWriter(relsEntry.Open()))
                    {
                        relsFileStream.WriteLine(RelsFile);
                    }

                    ZipArchiveEntry contentTypesEntry = archive.CreateEntry(ContentTypesFile_ArchiveEntryName);
                    using (StreamWriter contentTypesFileStream = new StreamWriter(contentTypesEntry.Open()))
                    {
                        contentTypesFileStream.WriteLine(ContentTypesFile);
                    }

                    ZipArchiveEntry modelEntry = archive.CreateEntry(ModelFile_ArchiveEntryName);
                    using (StreamWriter modelFileStream = new StreamWriter(modelEntry.Open()))
                    {
                        modelFileStream.WriteLine(modelFile);
                    }
                }
            }

            return destination.Length > 0;
        }
    }
}
