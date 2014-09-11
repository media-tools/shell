using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common.Util;

namespace Shell.Pictures
{
    public class PictureFile : MediaFile
    {
        public static HashSet<string> FILE_ENDINGS = new string[]{ ".png", ".jpg", ".gif", ".jpeg" }.ToHashSet();

        public PictureFile (FileInfo fileInfo, string root)
            : base (fileInfo, root)
        {
        }

        public static bool IsValidFile (FileInfo fileInfo)
        {
            return MediaFile.IsValidFile (fileInfo: fileInfo, fileEndings: FILE_ENDINGS);
        }
    }
}

