using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common.Util;

namespace Shell.Pictures
{
    public class VideoFile : MediaFile
    {
        public static HashSet<string> FILE_ENDINGS = new string[]{ ".mp4", ".avi", ".flv" }.ToHashSet ();

        public VideoFile (FileInfo fileInfo, string root)
            : base (fileInfo, root)
        {

        }

        public static bool IsValidFile (FileInfo fileInfo)
        {
            return MediaFile.IsValidFile (fileInfo: fileInfo, fileEndings: FILE_ENDINGS);
        }
    }
}

