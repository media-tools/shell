using System;
using System.Collections.Generic;
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

        public static string GetRelativePath (string fullPath, IRootDirectory share)
        {
            return fullPath.Substring (share.RootDirectory.Length).Trim ('/', '\\');
        }

        public static string GetAlbumPath (string fullPath, IRootDirectory share)
        {
            string relativePath = GetRelativePath (fullPath: fullPath, share: share);
            return Path.GetDirectoryName (relativePath).Trim ('/', '\\');
        }
    }
}

