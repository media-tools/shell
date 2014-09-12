using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shell.Pictures
{
    public class PictureFile : MediaFile
    {
        public static HashSet<string> FILE_ENDINGS = new string[]{ ".png", ".jpg", ".gif", ".jpeg" }.ToHashSet ();

        public PictureFile (string fullPath, string root)
            : base (fullPath, root)
        {
        }

        public static bool IsValidFile (FileInfo fileInfo)
        {
            return MediaFile.IsValidFile (fileInfo: fileInfo, fileEndings: FILE_ENDINGS);
        }

        public override void WriteAttributes (JsonWriter writer, MediaFile file, JsonSerializer serializer)
        {
        }

        public override void ReadAttributes (JObject jsonObject)
        {
        }
    }
}

