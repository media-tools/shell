using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Pictures.Files;

namespace Shell.Pictures.Content
{
    public class Video : Medium
    {
        public static HashSet<string> FILE_ENDINGS = new [] {
            ".mkv",
            ".mp4",
            ".avi",
            ".flv",
            ".mov",
            ".mpg",
            ".3gp",
            ".m4v",
            ".divx",
            ".webm",
            ".wmv"
        }.ToHashSet ();

        public static Dictionary<string[],string[]> MIME_TYPES = new Dictionary<string[],string[]> () {
            { new [] { "video/mp4" }, new [] { ".mp4" } },
            { new [] { "video/x-flv" }, new [] { ".flv" } },
            { new [] { "video/x-msvideo" }, new [] { ".avi" } },
            { new [] { "video/x-matroska" }, new [] { ".mkv" } },
            { new [] { "video/webm" }, new [] { ".webm" } },
            { new [] { "video/mpeg" }, new [] { ".mpg" } },
            { new [] { "video/ogg" }, new [] { ".ogv" } },
            { new [] { "video/x-ms-wmv" }, new [] { ".wmv" } },
            { new [] { "video/3gpp" }, new [] { ".3gp" } }
        };

        public static readonly string TYPE = "video";

        public override string Type { get { return TYPE; } }

        public Video (HexString hash)
            : base (hash)
        {
        }

        public Video (string fullPath)
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

