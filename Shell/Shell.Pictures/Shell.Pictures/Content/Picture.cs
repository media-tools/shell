using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Common.Util;
using Shell.Pictures.Files;

namespace Shell.Pictures.Content
{
    public class Picture : Medium
    {
        public static HashSet<string> FILE_ENDINGS = new [] {
            ".png",
            ".jpg",
            ".gif",
            ".jpeg",
            ".xcf",
            ".bmp",
            ".tiff",
            ".tif"
        }.ToHashSet ();

        public static Dictionary<string,string> MIME_TYPES = new Dictionary<string,string> () {
            { "image/jpeg", ".jpg" },
            { "image/png", ".png" },
            { "image/gif", ".gif" },
            { "image/svg+xml", ".svg" },
            { "image/tiff", ".tif" },
            { "image/x-ms-bmp", ".bmp" }
        };

        public static readonly string TYPE = "picture";

        private static PictureLibrary lib = new PictureLibrary ();

        public override string Type { get { return TYPE; } }

        public List<ExifTag> ExifTags = new List<ExifTag> ();

        public bool IsDateless { get; private set; }

        public Picture (HexString hash)
            : base (hash)
        {
            IsDateless = false;
        }

        public Picture (string fullPath)
            : base (fullPath: fullPath)
        {
            IsDateless = false;
        }

        public static bool IsValidFile (string fullPath)
        {
            return PictureShareUtilities.IsValidFile (fullPath: fullPath, fileEndings: FILE_ENDINGS);
        }

        public override void Index (string fullPath)
        {
            ExifTags = lib.GetExifTags (fullPath: fullPath);
            if (ExifTags.Count == 0) {
                string fileName = Path.GetFileName (fullPath);
                DateTime date;
                if (lib.GetFileNameDate (fileName: fileName, date: out date)) {
                    Log.Message ("Set exif date for picture: ", fullPath, " => ", string.Format ("{0:yyyy:MM:dd HH:mm:ss}", date));
                    lib.SetExifDate (fullPath: fullPath, date: date);
                    ExifTags = lib.GetExifTags (fullPath: fullPath);
                    IsDateless = false;
                } else {
                    IsDateless = true;
                }
            }
        }

        public override bool IsCompletelyIndexed {
            get {
                return ExifTags.Count > 0 || IsDateless;
            }
        }

        public override Dictionary<string, string> Serialize ()
        {
            // exif tags
            Dictionary<string, string> dict = new Dictionary<string, string> ();
            foreach (ExifTag tag in ExifTags) {
                string key;
                string value;
                if (tag.Serialize (out key, out value)) {
                    dict [key] = value;
                }
            }

            // is dateless?
            if (IsDateless) {
                dict ["flag:IsDateless"] = "true";
            } else {
                dict.Remove ("flag:IsDateless");
            }

            return dict;
        }

        public override void Deserialize (Dictionary<string, string> dict)
        {
            // exif tags
            ExifTags.Clear ();
            foreach (string key in dict.Keys) {
                string value = dict [key];
                ExifTag deserialized = null;
                if (ExifTag.Deserialize (key, value, out deserialized)) {
                    ExifTags.Add (deserialized);
                }
            }

            // is dateless?
            IsDateless = dict.ContainsKey ("flag:IsDateless") && dict ["flag:IsDateless"] == "true";
        }
    }
}
