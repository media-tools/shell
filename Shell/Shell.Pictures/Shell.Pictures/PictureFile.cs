using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shell.Pictures
{
    [JsonConverter (typeof(PictureFileConverter))]
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
    }

    public class PictureFileConverter : MediaFileConverter
    {
        protected override void WriteAttributes (JsonWriter writer, MediaFile file, JsonSerializer serializer)
        {
            writer.WritePropertyName ("Type");
            writer.WriteValue ("PictureFile");
        }

        protected override object Create (string fullPath, string root, JObject jsonObject)
        {
            return new PictureFile (fullPath: fullPath, root: root);
        }
    }
}

