using System;
using System.IO;
using System.Collections.Generic;

namespace Shell.Pictures
{
    public static class PictureShareUtilities
    {
        public static bool IsValidFile (string fullPath, HashSet<string> fileEndings)
        {
            return fileEndings.Contains (Path.GetExtension (fullPath).ToLower ());
        }

        public static string GetRelativePath (string fullPath, PictureShare share)
        {
            return fullPath.Substring (share.RootDirectory.Length).TrimStart ('/', '\\');
        }

        public static string GetAlbumPath (string fullPath, PictureShare share)
        {
            string relativePath = GetRelativePath (fullPath: fullPath, share: share);
            return Path.GetDirectoryName (relativePath).Trim ('/', '\\');
        }
    }
}

