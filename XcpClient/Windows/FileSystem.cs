using System;
using System.IO;

namespace XcpClient.Windows
{
    internal static class FileSystem
    {
        private static string _Directory;

        static FileSystem()
        {
            _Directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "xcp");
            if (!Directory.Exists(_Directory))
                Directory.CreateDirectory(_Directory);
        }

        public static string GetPath(string filename)
        {
            return Path.Combine(_Directory, filename);
        }
    }
}
