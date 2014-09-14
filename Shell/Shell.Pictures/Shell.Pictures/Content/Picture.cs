using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Pictures.Files;

namespace Shell.Pictures.Content
{
    public class Picture : Media
    {
        public static HashSet<string> FILE_ENDINGS = new string[]{ ".png", ".jpg", ".gif", ".jpeg" }.ToHashSet ();

        public static readonly string TYPE = "picture";

        public override string Type { get { return TYPE; } }

        public Picture (HexString hash)
            : base (hash)
        {
        }

        public Picture (string fullPath)
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

