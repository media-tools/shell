using System;
using System.IO;
using Shell.Common.IO;
using Shell.Common.Util;

namespace Shell.Media.Files
{
    public static class FileHooks
    {
        private static MediaFileLibrary lib = new MediaFileLibrary ();

        public static void RunIndexHooks (ref string fullPath)
        {
            // does the file have no proper filename?
            if (NamingUtilities.HasNoFileEnding (fullPath: fullPath)) {
                string fileEnding;
                // determine the best file ending
                if (DetermineFileEndingByExternalCall (fullPath: fullPath, fileEnding: out fileEnding)) {
                    // rename the file
                    AddFileEnding (fullPath: ref fullPath, fileEnding: fileEnding);
                }
            }

            // is the file ending not completely lower case?
            ChangeFileEndingToLowerCase (fullPath: ref fullPath);

            // does the file name contain characters that are not allowed in NTFS?
            RenameFilePlatformIndependent (fullPath: ref fullPath);
        }

        public static bool RenameFilePlatformIndependent (ref string fullPath)
        {
            if (Path.GetFileName (fullPath).Contains (":")) {
                string oldPath = fullPath;
                string newPath = Path.GetDirectoryName (oldPath) + SystemInfo.PathSeparator + Path.GetFileName (oldPath).Replace (":", "_");
                return RenamePath (fullPath: ref fullPath, oldPath: oldPath, newPath: newPath);
            }
            if (Path.GetFileName (fullPath).StartsWith ("_")) {
                string oldPath = fullPath;
                string newPath = Path.GetDirectoryName (oldPath) + SystemInfo.PathSeparator + Path.GetFileName (oldPath).Trim ('_');
                return RenamePath (fullPath: ref fullPath, oldPath: oldPath, newPath: newPath);
            }
            return false;
        }

        public static bool ChangeFileEndingToLowerCase (ref string fullPath)
        {
            if (Path.GetFileNameWithoutExtension (fullPath).Length >= 1 && Path.GetExtension (fullPath) != Path.GetExtension (fullPath).ToLower ()) {
                string oldPath = fullPath;
                string newPath = Path.GetDirectoryName (oldPath) + SystemInfo.PathSeparator + Path.GetFileNameWithoutExtension (oldPath) + Path.GetExtension (oldPath).ToLower ();
                return RenamePath (fullPath: ref fullPath, oldPath: oldPath, newPath: newPath);
            }
            return false;
        }

        public static bool RenamePath (ref string fullPath, string oldPath, string newPath)
        {
            try {
                Log.Info ("Rename file: ", Path.GetFileName (oldPath), " => ", Path.GetFileName (newPath));
                if (File.Exists (newPath) && File.Exists (oldPath)) {
                    File.Delete (newPath);
                }
                File.Move (sourceFileName: oldPath, destFileName: newPath);
                fullPath = newPath;
                return true;
            } catch (IOException ex) {
                Log.Error (ex);
            }
            return false;
        }


        public static bool DetermineFileEndingByExternalCall (string fullPath, out string fileEnding)
        {
            string mimeType = lib.GetMimeTypeByExternalCall (fullPath: fullPath);
            fileEnding = lib.GetExtensionByMimeType (mimeType: mimeType);

            Log.Debug ("DetermineFileEnding: mimeType=", mimeType, ", fileEnding=", fileEnding);

            return fileEnding != null;
        }

        public static bool DetermineFileEndingByMimeType (string mimeType, out string fileEnding)
        {
            fileEnding = lib.GetExtensionByMimeType (mimeType: mimeType);

            Log.Debug ("DetermineFileEnding: mimeType=", mimeType, ", fileEnding=", fileEnding);

            return fileEnding != null;
        }

        public static void AddFileEnding (ref string fullPath, string fileEnding)
        {
            string oldFullPath = fullPath;
            string newFullPath = fullPath + fileEnding;
            File.Move (sourceFileName: oldFullPath, destFileName: newFullPath);
            fullPath = newFullPath;

            Log.Info ("Rename file: old full path = ", oldFullPath);
            Log.Info ("             new full path = ", newFullPath);
        }
    }
}

