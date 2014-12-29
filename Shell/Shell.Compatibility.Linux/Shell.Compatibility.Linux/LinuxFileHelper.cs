using System;
using System.IO;
using Shell.Compatibility;
using Mono.Unix;
using Shell.Common.IO;

namespace Shell.Compatibility.Linux
{
    class LinuxFileHelper : FileHelper
    {
        static UnixFileSystemInfo GetUnixFileInfo (string path)
        {
            try {
                return Mono.Unix.UnixFileInfo.GetFileSystemEntry (path);
            } catch (DirectoryNotFoundException ex) {
                // If we have a file /foo/bar and probe the path /foo/bar/baz, we get a DirectoryNotFound exception
                // because 'bar' is a file and therefore 'baz' cannot possibly exist. This is annoying.
                var inner = ex.InnerException as UnixIOException;
                if (inner != null && inner.ErrorCode == Mono.Unix.Native.Errno.ENOTDIR)
                    return null;
                throw;
            }
        }

        public override bool IsSymLink (string path)
        {
            UnixSymbolicLinkInfo i = new UnixSymbolicLinkInfo (path);
            switch (i.FileType) {
            case FileTypes.SymbolicLink:
                return true;
            case FileTypes.Fifo:
            case FileTypes.Socket:
            case FileTypes.BlockDevice:
            case FileTypes.CharacterDevice:
            case FileTypes.Directory:
            case FileTypes.RegularFile:
            default:
                return false;
            }
        }

        public override bool CreateSymLink (string target, string symLink)
        {
            try {
                UnixFileInfo targetFile = new UnixFileInfo (target);
                targetFile.CreateSymbolicLink (symLink);
                return true;
            } catch (Exception ex) {
                Log.Error ("Failed to create symbolic link from '", symLink, "' to '", target, "'");
                Log.Error (ex);
                return false;
            }
        }

        public override string ReadSymLink (string path)
        {
            UnixSymbolicLinkInfo i = new UnixSymbolicLinkInfo (path);
            switch (i.FileType) {
            case FileTypes.SymbolicLink:
                try {
                    return i.GetContents ().FullName;
                } catch (Exception ex) {
                    Log.Error ("Failed to read symbolic link: '", path, "'");
                    Log.Error (ex);
                    return null;
                }
            case FileTypes.Fifo:
            case FileTypes.Socket:
            case FileTypes.BlockDevice:
            case FileTypes.CharacterDevice:
            case FileTypes.Directory:
            case FileTypes.RegularFile:
            default:
                return null;
            }
        }

        /*

        public override bool CanExecute (string path)
        {
            UnixFileInfo fi = new UnixFileInfo (path);
            if (!fi.Exists)
                return false;
            return 0 != (fi.FileAccessPermissions & (FileAccessPermissions.UserExecute | FileAccessPermissions.GroupExecute | FileAccessPermissions.OtherExecute));
        }

        public override bool CanWrite (string path)
        {
            var info = GetUnixFileInfo (path);
            return info != null && info.CanAccess (Mono.Unix.Native.AccessModes.W_OK);
        }

        public override bool Delete (string path)
        {
            var info = GetUnixFileInfo (path);
            if (info != null && info.Exists) {
                try {
                    info.Delete ();
                    return true;
                } catch {
                    // If the directory is not empty we return false. JGit relies on this
                    return false;
                }
            }
            return false;
        }

        public override bool Exists (string path)
        {
            var info = GetUnixFileInfo (path);
            return info != null && info.Exists;
        }

        public override bool IsDirectory (string path)
        {
            try {
                var info = GetUnixFileInfo (path);
                return info != null && info.Exists && info.FileType == FileTypes.Directory;
            } catch (DirectoryNotFoundException) {
                // If the file /foo/bar exists and we query to see if /foo/bar/baz exists, we get a
                // DirectoryNotFound exception for Mono.Unix. In this case the directory definitely
                // does not exist.
                return false;
            }
        }

        public override bool IsFile (string path)
        {
            var info = GetUnixFileInfo (path);
            return info != null && info.Exists && (info.FileType == FileTypes.RegularFile || info.FileType == FileTypes.SymbolicLink);
        }

        public override long LastModified (string path)
        {
            var info = GetUnixFileInfo (path);
            return info != null && info.Exists ? info.LastWriteTimeUtc.ToMillisecondsSinceEpoch() : 0;
        }

        public override long Length (string path)
        {
            var info = GetUnixFileInfo (path);
            return info != null && info.Exists ? info.Length : 0;
        }

        public override void MakeFileWritable (string file)
        {
            var info = GetUnixFileInfo (file);
            if (info != null)
                info.FileAccessPermissions |= (FileAccessPermissions.GroupWrite | FileAccessPermissions.OtherWrite | FileAccessPermissions.UserWrite);
        }

        public override bool SetExecutable (string path, bool exec)
        {
            UnixFileInfo fi = new UnixFileInfo (path);
            FileAccessPermissions perms = fi.FileAccessPermissions;
            if (exec) {
                if (perms.HasFlag (FileAccessPermissions.UserRead))
                    perms |= FileAccessPermissions.UserExecute;
                if (perms.HasFlag (FileAccessPermissions.OtherRead))
                    perms |= FileAccessPermissions.OtherExecute;
                if ((perms.HasFlag (FileAccessPermissions.GroupRead)))
                    perms |= FileAccessPermissions.GroupExecute;
            } else {
                if (perms.HasFlag (FileAccessPermissions.UserRead))
                    perms &= ~FileAccessPermissions.UserExecute;
                if (perms.HasFlag (FileAccessPermissions.OtherRead))
                    perms &= ~FileAccessPermissions.OtherExecute;
                if ((perms.HasFlag (FileAccessPermissions.GroupRead)))
                    perms &= ~FileAccessPermissions.GroupExecute;
            }
            fi.FileAccessPermissions = perms;
            return true;
        }

        public override bool SetReadOnly (string path)
        {
            try {
                var info = GetUnixFileInfo (path);
                if (info != null)
                    info.FileAccessPermissions &= ~ (FileAccessPermissions.GroupWrite | FileAccessPermissions.OtherWrite | FileAccessPermissions.UserWrite);
                return true;
            } catch {
                return false;
            }
        }*/
    }
}

