using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Control.Common.IO;
using Control.Compatibility;

namespace Control.FileSync
{
    public class FileSystemLibrary : Library
    {
        public static IEnumerable<FileInfo> GetFileList (string rootDirectory, Func<FileInfo, bool> fileFilter, Func<DirectoryInfo, bool> dirFilter)
        {
            ProgressBar progressBar = Log.OpenProgressBar (identifier: "FileSystemLibrary:" + rootDirectory, description: "Searching for shares...");
            Func<FileInfo, bool> _fileFilter = info => fileFilter (info);
            Func<DirectoryInfo, bool> _dirFilter = info => dirFilter (info) && FilterSystemPath (info.FullName) && FilterCustomPath (info.FullName);
            DirectoryInfo root = new DirectoryInfo (rootDirectory);
            IEnumerable<FileInfo> result = GetFileList (rootDirectory: root, fileFilter: _fileFilter, dirFilter: _dirFilter, progressBar : progressBar);
            return result;
        }

        public static bool FilterSystemPath (string path)
        {
            return !path.StartsWith ("/proc") && !path.StartsWith ("/sys")
                && !path.StartsWith ("/run") && !path.StartsWith ("/tmp") && !path.StartsWith ("/boot")
                && !path.StartsWith ("/lib") && !path.StartsWith ("/sbin") && !path.StartsWith ("/bin")
                && !path.StartsWith ("/dev") && !path.StartsWith ("/cdrom") && !path.StartsWith ("/srv") 
                && !path.StartsWith ("/var") && !path.StartsWith ("/usr/share") && !path.StartsWith ("/usr/lib") 
                && !path.StartsWith ("/usr/src") && !path.StartsWith ("/usr/bin") && !path.StartsWith ("/usr/sbin")
                && !path.StartsWith ("/usr/include") && !path.Contains ("lost+found");
        }

        public static bool FilterCustomPath (string path)
        {
            return !path.EndsWith (".git") && !path.EndsWith ("HardLinks");
        }

        private static IEnumerable<FileInfo> GetFileList (DirectoryInfo rootDirectory, Func<FileInfo, bool> fileFilter, Func<DirectoryInfo, bool> dirFilter, ProgressBar progressBar, int depth = 0)
        {
            IEnumerable<FileInfo> fileList = null;
            try {
                fileList = rootDirectory.GetFiles ();
            } catch (UnauthorizedAccessException ex) {
                Log.DebugLog ("UnauthorizedAccessException: " + ex.Message);
                yield break;
            }
            if (fileList != null) {
                foreach (FileInfo file in fileList) {
                    progressBar.Next ();
                    if (fileFilter (file)) {
                        if (FileHelper.Instance.IsSymLink (file)) {
                            //Log.Error ("Symbolic Link: " + file);
                        } else {
                            yield return file;
                        }
                    }
                }
            }


            IEnumerable<DirectoryInfo> directoryList = null;
            try {
                directoryList = rootDirectory.GetDirectories ();
            } catch (UnauthorizedAccessException ex) {
                Log.DebugLog ("UnauthorizedAccessException: " + ex.Message);
                yield break;
            }
            if (directoryList != null) {
                foreach (DirectoryInfo subDirectory in directoryList) {
                    if (dirFilter (subDirectory)) {
                        if (FileHelper.Instance.IsSymLink (subDirectory)) {
                            Log.DebugLog ("Symbolic Link: " + subDirectory);
                        } else {
                            foreach (FileInfo file in GetFileList(rootDirectory: subDirectory, fileFilter: fileFilter, dirFilter: dirFilter, depth: depth + 1, progressBar: progressBar)) {
                                yield return file;
                            }
                        }
                    }
                }
            }

            if (depth == 0) {
                progressBar.Finish ();
            }
        }
    }
}

