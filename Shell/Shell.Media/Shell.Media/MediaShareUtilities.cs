using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Shell.Common.Shares;

namespace Shell.Media
{
    public static class MediaShareUtilities
    {
        public static bool IsValidFile (string fullPath, HashSet<string> fileEndings)
        {
            return fileEndings.Contains (Path.GetExtension (fullPath).ToLower ());
        }

        public static string GetRelativePath (string fullPath, IRootDirectory rootDirectoryHolder)
        {
            Debug.Assert (fullPath.StartsWith (rootDirectoryHolder.RootDirectory),
                "file path is not in root directory (fullPath=" + fullPath + ",root=" + rootDirectoryHolder.RootDirectory + ")");

            return fullPath.Substring (rootDirectoryHolder.RootDirectory.Length).Trim ('/', '\\');
        }

        public static string GetAlbumPath (string fullPath, IRootDirectory rootDirectoryHolder)
        {
            string relativePath = GetRelativePath (fullPath: fullPath, rootDirectoryHolder: rootDirectoryHolder);
            return Path.GetDirectoryName (relativePath).Trim ('/', '\\');
        }

        public static string GetAlbumPath (string relativePath)
        {
            return Path.GetDirectoryName (relativePath).Trim ('/', '\\');
        }
    }
}

