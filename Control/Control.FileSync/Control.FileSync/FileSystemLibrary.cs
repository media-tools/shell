using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Control.FileSync
{
    public static class FileSystemLibrary
    {
        
        private static object lockThis = new object ();
        private static List<string> _files = null;

        public static IEnumerable<FileInfo> GetFileList (string rootDirectory, Func<FileInfo, bool> filter)
        {
            DirectoryInfo root = new DirectoryInfo (rootDirectory);
            return GetFileList (rootDirectory: root, filter: filter);
        }

        private static IEnumerable<FileInfo> GetFileList (DirectoryInfo rootDirectory, Func<FileInfo, bool> filter)
        {
            foreach (FileInfo file in rootDirectory.GetFiles()) {
                if (filter (file)) {
                    yield return file;
                }
            }
            foreach (DirectoryInfo subDirectory in rootDirectory.GetDirectories()) {
                foreach (FileInfo file in GetFileList(rootDirectory: subDirectory, filter: filter)) {
                    yield return file;
                }
            }
        }
    }
}

