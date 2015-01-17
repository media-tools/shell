using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Media.Files;

namespace Shell.Media.Content
{
    public class Document : Medium
    {
        public static HashSet<string> FILE_ENDINGS = new [] {
            ".pdf",
            ".txt",
            ".rtf",
            ".eml",
            ".nfo",
            ".html",
            ".htm",
            ".chm",
            ".vcf",
            ".sh",
            ".ini",
            ".gz",
            ".xz",
            ".tar",

            // genealogy
            ".ahn",
            ".ged",
            ".gpkg",

        }.ToHashSet ();

        public static Dictionary<string[],string[]> MIME_TYPES = new Dictionary<string[],string[]> () {
            { new [] { "application/pdf" }, new [] { ".pdf" } },
            { new [] { "application/rtf", "text/rtf" }, new [] { ".rtf" } },
            { new [] { "text/plain" }, new [] { ".txt" } },
            { new [] { "text/x-markdown" }, new [] { ".md" } },
            { new [] { "message/rfc822" }, new [] { ".eml" } },
            { new [] { "text/html" }, new [] { ".html", ".htm" } },
            { new [] { "application/x-chm" }, new [] { ".chm" } },
            { new [] { "text/x-vcard" }, new [] { ".vcf" } },
            { new [] { "text/x-shellscript" }, new [] { ".sh" } },
            { new [] { "text/x-ini" }, new [] { ".ini" } },
            { new [] { "application/gzip" }, new [] { ".gz" } },
            { new [] { "application/x-xz" }, new [] { ".xz" } },
            { new [] { "application/x-tar" }, new [] { ".tar" } },
            { new [] { "application/x-genealogy" }, new [] { ".ahn", ".ged", ".gpkg" } },
        };

        public static readonly string TYPE = "document";

        public override string Type { get { return TYPE; } }

        public override DateTime? PreferredTimestamp { get { return null; } }

        public Document (HexString hash)
            : base (hash)
        {
        }

        public static bool IsValidFile (string fullPath)
        {
            return MediaShareUtilities.IsValidFile (fullPath: fullPath, fileEndings: FILE_ENDINGS);
        }

        public override void Index (string fullPath)
        {
            if (string.IsNullOrWhiteSpace (MimeType)) {
                MimeType = libMediaFile.GetMimeTypeByExtension (fullPath: fullPath);
            }
        }

        public override bool IsCompletelyIndexed {
            get {
                return !string.IsNullOrWhiteSpace (MimeType);
            }
        }

        public static void RunIndexHooks (ref string fullPath)
        {
        }

        protected override void SerializeInternal (Dictionary<string, string> dict)
        {
        }

        protected override void DeserializeInternal (Dictionary<string, string> dict)
        {
        }
    }
}

