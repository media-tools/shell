using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Pictures.Files;

namespace Shell.Pictures.Content
{
    public class Audio : Media
    {
        public static HashSet<string> FILE_ENDINGS = new string[]{ ".mp3", ".wav", ".ogg", ".aac" }.ToHashSet ();

        public static readonly string TYPE = "audio";

        public override string Type { get { return TYPE; } }

        public Audio (HexString hash)
            : base (hash)
        {
        }

        public Audio (string fullPath)
            : base (fullPath: fullPath)
        {
        }

        public static bool IsValidFile (string fullPath)
        {
            return PictureShareUtilities.IsValidFile (fullPath: fullPath, fileEndings: FILE_ENDINGS);
        }

        public override void WriteAttributes (JsonWriter writer, JsonSerializer serializer)
        {
        }

        public override void ReadAttributes (JObject jsonObject)
        {
        }
    }
}

