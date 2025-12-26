using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ThreeDModelFiles
{
    public partial class STLFile
    {
        internal static class SaveHelper
        {
            public static string GetFileHeaderString()
            {
                if (HeaderString == null)
                {
                    System.Diagnostics.FileVersionInfo versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                    string version = $"v{versionInfo.FileVersion}";
                    HeaderString = $"Exported from 3dModelFiles CSharp Library {version}";
                }
                return HeaderString;
            }
            private static string HeaderString = null;
        }

    }
}
