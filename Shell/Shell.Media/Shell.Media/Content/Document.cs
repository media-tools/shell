using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.Util;
using Shell.Media.Database;
using Shell.Media.Files;

namespace Shell.Media.Content
{
    public class Document : MediaFile
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
            ".odt",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".pps",
            ".ppsx",

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
            { new [] { "application/vnd.oasis.opendocument.text" }, new [] { ".odt" } },

            // doc
            { new [] { "application/msword" }, new [] { ".doc" } }, { // docx
                new [] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                new [] { ".docx" }
            },
            // xls
            { new [] { "application/vnd.ms-excel" }, new [] { ".xls" } }, { // xlsx
                new [] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                new [] { ".xlsx" }
            },
            // pps
            { new [] { "application/vnd.ms-powerpoint" }, new [] { ".pps" } }, { // ppsx
                new [] { "application/vnd.openxmlformats-officedocument.presentationml.slideshow" },
                new [] { ".ppsx" }
            },

            { new [] { "application/x-genealogy" }, new [] { ".ahn", ".ged", ".gpkg" } },
        };

        public override DateTime? PreferredTimestampInternal { get { return null; } }

        public Document (string fullPath, MediaDatabase database)
            : base (fullPath, database)
        {
        }

        public Document ()
        {
        }

        public static new bool IsValidFile (string fullPath)
        {
            return MediaShareUtilities.IsValidFile (fullPath: fullPath, fileEndings: FILE_ENDINGS);
        }

        public override void Index ()
        {
            if (string.IsNullOrWhiteSpace (MimeType)) {
                MimeType = libMediaFile.GetMimeTypeByExtension (fullPath: FullPath);
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

