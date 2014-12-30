using System;
using System.IO;
using System.Collections.Generic;

namespace Shell.Media
{
    public static class MediaShareUtilities
    {
        public static bool IsValidFile (string fullPath, HashSet<string> fileEndings)
        {
            return fileEndings.Contains (Path.GetExtension (fullPath).ToLower ());
        }

        public static string GetRelativePath (string fullPath, MediaShare share)
        {
            return fullPath.Substring (share.RootDirectory.Length).Trim ('/', '\\');
        }

        public static string GetAlbumPath (string fullPath, MediaShare share)
        {
            string relativePath = GetRelativePath (fullPath: fullPath, share: share);
            return Path.GetDirectoryName (relativePath).Trim ('/', '\\');
        }
    }
}

