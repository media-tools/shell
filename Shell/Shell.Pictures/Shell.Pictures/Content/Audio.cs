using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Pictures.Files;

namespace Shell.Pictures.Content
{
    public class Audio : Medium
    {
        public static HashSet<string> FILE_ENDINGS = new string[]{ ".mp3", ".wav", ".ogg", ".aac", ".m4a" }.ToHashSet ();

        public static Dictionary<string,string> MIME_TYPES = new Dictionary<string,string> () {
            { "audio/mpeg", ".mp3" },
            { "audio/x-wav", ".wav" },
            { "audio/ogg", ".ogg" },
            { "audio/x-ms-wma", ".wma" },
            { "audio/flac", ".flac" }
        };

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

        public override void Index (string fullPath)
        {
        }

        public override bool IsCompletelyIndexed {
            get {
                return true;
            }
        }

        public override Dictionary<string, string> Serialize ()
        {
            return new Dictionary<string, string> ();
        }

        public override void Deserialize (Dictionary<string, string> dict)
        {
        }
    }
}

