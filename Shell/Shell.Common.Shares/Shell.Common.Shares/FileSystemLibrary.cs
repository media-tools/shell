using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Compatibility;
using Shell.Common.Util;

namespace Shell.Common.Shares
{
    public class FileSystemLibrary : Library
    {
        public static IEnumerable<FileInfo> GetFileList (string rootDirectory, Func<FileInfo, bool> fileFilter, Func<DirectoryInfo, bool> dirFilter, bool followSymlinks)
        {
            ProgressBar progressBar = Log.OpenProgressBar (identifier: "FileSystemLibrary:" + rootDirectory, description: "Searching for shares...");
            Func<FileInfo, bool> _fileFilter = info => fileFilter (info);
            Func<DirectoryInfo, bool> _dirFilter = info => dirFilter (info) && FilterSystemPath (info.FullName) && FilterCustomPath (info.FullName);
            DirectoryInfo root = new DirectoryInfo (rootDirectory);
            IEnumerable<FileInfo> result = GetFileList (rootDirectory: root, fileFilter: _fileFilter, dirFilter: _dirFilter, progressBar: progressBar, followSymlinks: followSymlinks);
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

        private static IEnumerable<FileInfo> GetFileList (DirectoryInfo rootDirectory, Func<FileInfo, bool> fileFilter, Func<DirectoryInfo, bool> dirFilter, ProgressBar progressBar, bool followSymlinks, int depth = 0)
        {
            // Log.Debug (rootDirectory.FullName);

            // list files
            IEnumerable<FileInfo> fileList = null;
            try {
                fileList = from file in rootDirectory.GetFiles ()
                                       orderby file.Name
                                       select file;
            } catch (UnauthorizedAccessException ex) {
                Log.DebugLog ("UnauthorizedAccessException: " + ex.Message);
                yield break;
            }
            if (fileList != null) {
                foreach (FileInfo file in fileList) {
                    progressBar.Next ();
                    if (fileFilter (file)) {
                        if (FileHelper.Instance.IsSymLink (file)) {
                            Log.Error ("Symbolic Link: " + file);
                        } else {
                            yield return file;
                        }
                    }
                }
            }

            // list directories
            IEnumerable<DirectoryInfo> directoryList = null;
            try {
                directoryList = from dir in rootDirectory.GetDirectories ()
                                            orderby dir.Name
                                            select dir;
            } catch (UnauthorizedAccessException ex) {
                Log.DebugLog ("UnauthorizedAccessException: " + ex.Message);
                yield break;
            }
            if (directoryList != null) {
                foreach (DirectoryInfo subDirectory in directoryList) {
                    if (dirFilter (subDirectory)) {
                        if (!followSymlinks && FileHelper.Instance.IsSymLink (subDirectory)) {
                            Log.DebugLog ("Symbolic Link: " + subDirectory);
                        } else {
                            foreach (FileInfo file in GetFileList(rootDirectory: subDirectory, fileFilter: fileFilter, dirFilter: dirFilter, followSymlinks: followSymlinks, depth: depth + 1, progressBar: progressBar)) {
                                yield return file;
                            }
                        }
                    }
                }
            }

            // done?
            if (depth == 0) {
                progressBar.Finish ();
            }
        }

        public static IEnumerable<DirectoryInfo> GetDirectoryList (string rootDirectory, Func<DirectoryInfo, bool> dirFilter, bool followSymlinks, bool returnSymlinks)
        {
            ProgressBar progressBar = Log.OpenProgressBar (identifier: "FileSystemLibrary:" + rootDirectory, description: "Searching for directories...");
            Func<DirectoryInfo, bool> _dirFilter = info => dirFilter (info) && FilterSystemPath (info.FullName) && FilterCustomPath (info.FullName);
            DirectoryInfo root = new DirectoryInfo (rootDirectory);
            IEnumerable<DirectoryInfo> result = GetDirectoryList (rootDirectory: root, dirFilter: _dirFilter, progressBar: progressBar, followSymlinks: followSymlinks, returnSymlinks: returnSymlinks);
            return result;
        }

        private static IEnumerable<DirectoryInfo> GetDirectoryList (DirectoryInfo rootDirectory, Func<DirectoryInfo, bool> dirFilter, ProgressBar progressBar, bool followSymlinks, bool returnSymlinks, int depth = 0)
        {
            // return the current directory
            yield return rootDirectory;

            // list directories
            IEnumerable<DirectoryInfo> directoryList = null;
            try {
                directoryList = from dir in rootDirectory.GetDirectories ()
                                            orderby dir.Name
                                            select dir;
            } catch (UnauthorizedAccessException ex) {
                Log.DebugLog ("UnauthorizedAccessException: " + ex.Message);
                yield break;
            }
            if (directoryList != null) {
                foreach (DirectoryInfo subDirectory in directoryList) {
                    progressBar.Next ();
                    if (dirFilter (subDirectory)) {
                        if (FileHelper.Instance.IsSymLink (subDirectory)) {
                            if (returnSymlinks) {
                                yield return subDirectory;
                            }
                            if (!followSymlinks) {
                                //Log.Debug ("Symbolic Link: " + subDirectory);
                                continue;
                            }
                        }
                        foreach (DirectoryInfo subSubDirectory in GetDirectoryList(rootDirectory: subDirectory, dirFilter: dirFilter, followSymlinks: followSymlinks, returnSymlinks: returnSymlinks, depth: depth + 1, progressBar: progressBar)) {
                            yield return subSubDirectory;
                        }
                    }
                }
            }

            // list dead symlinks
            IEnumerable<FileInfo> fileList = null;
            try {
                fileList = from file in rootDirectory.GetFiles ()
                                       orderby file.Name
                                       select file;
            } catch (UnauthorizedAccessException ex) {
                Log.DebugLog ("UnauthorizedAccessException: " + ex.Message);
                yield break;
            }
            if (fileList != null) {
                foreach (FileInfo file in fileList) {
                    if (FileHelper.Instance.IsSymLink (file)) {
                        string target = FileHelper.Instance.ReadSymLink (file.FullName);
                        if (target != null && !File.Exists (target) && !Directory.Exists (target)) {
                            progressBar.Next ();
                            Log.Debug ("Dead symbolic Link: " + file);
                            yield return new DirectoryInfo (file.FullName);
                        }
                    }
                }
            }

            // done?
            if (depth == 0) {
                progressBar.Finish ();
            }
        }
    }
}

