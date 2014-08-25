using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Control.Common.IO;
using Control.Compatibility.Common;

namespace Control.FileSync
{
    public static class FileSystemLibrary
    {
        public static IEnumerable<FileInfo> GetFileList (string rootDirectory, Func<FileInfo, bool> fileFilter, Func<DirectoryInfo, bool> dirFilter)
        {
            Func<FileInfo, bool> _fileFilter = info => fileFilter (info);// && !info.FullName.StartsWith ("/proc");
            Func<DirectoryInfo, bool> _dirFilter = info => dirFilter (info) && FilterSystemPath (info.FullName) && FilterCustomPath (info.FullName);
            DirectoryInfo root = new DirectoryInfo (rootDirectory);
            return GetFileList (rootDirectory: root, fileFilter: _fileFilter, dirFilter: _dirFilter);
        }

        public static bool FilterSystemPath (string path)
        {
            return !path.StartsWith ("/proc") && !path.StartsWith ("/sys")
                && !path.StartsWith ("/run") && !path.StartsWith ("/tmp") && !path.StartsWith ("/boot")
                && !path.StartsWith ("/lib") && !path.StartsWith ("/sbin") && !path.StartsWith ("/bin")
                && !path.StartsWith ("/dev") && !path.StartsWith ("/cdrom") && !path.StartsWith ("/srv") 
                && !path.StartsWith ("/var") && !path.StartsWith ("/usr/share") && !path.StartsWith ("/usr/lib") 
                && !path.StartsWith ("/usr/src") && !path.StartsWith ("/usr/bin") && !path.StartsWith ("/usr/sbin")
                && !path.StartsWith ("/usr/include");
        }

        public static bool FilterCustomPath (string path)
        {
            return !path.EndsWith (".git") && !path.EndsWith ("HardLinks");
        }

        private static IEnumerable<FileInfo> GetFileList (DirectoryInfo rootDirectory, Func<FileInfo, bool> fileFilter, Func<DirectoryInfo, bool> dirFilter)
        {
            IEnumerable<FileInfo> fileList = null;
            try {
                fileList = rootDirectory.GetFiles ();
            } catch (UnauthorizedAccessException ex) {
                Log.Error ("UnauthorizedAccessException: " + ex.Message);
                yield break;
            }
            if (fileList != null) {
                foreach (FileInfo file in fileList) {
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
                Log.Error ("UnauthorizedAccessException: " + ex.Message);
                yield break;
            }
            if (directoryList != null) {
                foreach (DirectoryInfo subDirectory in directoryList) {
                    if (dirFilter (subDirectory)) {
                        if (FileHelper.Instance.IsSymLink (subDirectory)) {
                            Log.Error ("Symbolic Link: " + subDirectory);
                        } else {
                            foreach (FileInfo file in GetFileList(rootDirectory: subDirectory, fileFilter: fileFilter, dirFilter: dirFilter)) {
                                yield return file;
                            }
                        }
                    }
                }
            }
        }
        /*public bool IsSymLink(string path) {
        Mono.Unix.UnixSymbolicLinkInfo i = new Mono.Unix.UnixSymbolicLinkInfo( path );
        switch( i.FileType )
        {
            case FileTypes.SymbolicLink:
            case FileTypes.Fifo:
            case FileTypes.Socket:
            case FileTypes.BlockDevice:
            case FileTypes.CharacterDevice:
            case FileTypes.Directory:
            case FileTypes.RegularFile:
            }
        }*/
    }
}

