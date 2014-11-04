using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Pictures.Files;

namespace Shell.Pictures.Content
{
    public class Document : Medium
    {
        public static HashSet<string> FILE_ENDINGS = new []{ ".pdf", ".txt", ".rtf", ".eml", ".nfo" }.ToHashSet ();

        public static Dictionary<string,string> MIME_TYPES = new Dictionary<string,string> () {
            { "application/pdf", ".pdf" },
            { "application/rtf", ".rtf" },
            { "text/rtf", ".rtf" },
            { "text/plain", ".txt" },
            { "message/rfc822", ".eml" }
        };

        public static readonly string TYPE = "document";

        public override string Type { get { return TYPE; } }

        public Document (HexString hash)
            : base (hash)
        {
        }

        public Document (string fullPath)
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

