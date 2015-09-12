using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;

//using BitMiracle.LibJpeg;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Common.Util;
using Shell.Media.Database;
using Shell.Media.Files;
using Shell.Media.Pictures;
using SQLite;
using BitMiracle.LibJpeg;

namespace Shell.Media.Content
{
    public class Picture : MediaFile
    {
        public static HashSet<string> FILE_ENDINGS = new [] {
            ".png",
            ".jpg",
            ".gif",
            ".jpeg",
            ".xcf",
            ".bmp",
            ".tiff",
            ".tif",
            ".ico",
            ".pamp",
        }.ToHashSet ();

        public static Dictionary<string[],string[]> MIME_TYPES = new Dictionary<string[],string[]> () {
            { new [] { "image/jpeg" }, new [] { ".jpg", ".jpeg", ".pamp" } },
            { new [] { "image/png" }, new [] { ".png" } },
            { new [] { "image/gif" }, new [] { ".gif" } },
            { new [] { "image/svg+xml" }, new [] { ".svg" } },
            { new [] { "image/tiff" }, new [] { ".tif", ".tiff" } },
            { new [] { "image/x-ms-bmp" }, new [] { ".bmp" } },
            { new [] { "image/x-icon", "image/vnd.microsoft.icon" }, new [] { ".ico" } },
            { new [] { "image/x-xcf" }, new [] { ".xcf" } },
        };

        private static PictureLibrary lib = new PictureLibrary ();

        //[TextBlob ("ExifTagsBlobbed")]
        public string SqlExifTags {
            get { return ExifTagExtensions.Serialize (ExifTags); }
            set { ExifTags = ExifTagExtensions.Deserialize (value); }
        }

        public List<ExifTag> ExifTags = new List<ExifTag> ();

        public DateTime? ExifTimestampCreated {
            get {
                string[] possibleTagNames = new [] {
                    "DateTimeOriginal",
                    "CreateDate",
                    "GPSDateTime",
                    //"ModifyDate",
                    "DateTime",
                };
                return lib.TryParseExifTimestamp (exifTags: ExifTags, possibleTagNames: possibleTagNames);
            }
        }

        public DateTime? ExifTimestampModified {
            get {
                string[] possibleTagNames = new [] {
                    "ModifyDate",
                };
                return lib.TryParseExifTimestamp (exifTags: ExifTags, possibleTagNames: possibleTagNames);
            }
        }

        public DateTime? ExifTimestampAcquired {
            get {
                string[] possibleTagNames = new [] {
                    "DateAcquired",
                };
                return lib.TryParseExifTimestamp (exifTags: ExifTags, possibleTagNames: possibleTagNames);
            }
        }

        public override DateTime? PreferredTimestampInternal {
            get {
                DateTime? preferred = null;
                if (ExifTimestampCreated.HasValue)
                    preferred = ExifTimestampCreated;
                else if (ExifTimestampModified.HasValue)
                    preferred = ExifTimestampModified;
                else if (ExifTimestampAcquired.HasValue)
                    preferred = ExifTimestampAcquired;

                if (preferred.HasValue) {
                    if (preferred.Value.Year == 2006 && preferred.Value.Month == 8 && preferred.Value.Day == 2) {
                        preferred = null;
                    }
                }
                   
                return preferred;
            }
        }

        public bool IsDateless { get; set; }

        public bool IsCommonFormat { get; set; }

        public string SqlPixelHash { get { return PixelHash.Hash; } set { PixelHash = new HexString { Hash = value }; } }

        [Ignore]
        public HexString PixelHash { get; private set; }

        public Picture (string fullPath, MediaDatabase database)
            : base (fullPath, database)
        {
            IsDateless = false;
        }

        public Picture ()
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
            IsCommonFormat = MimeType != "image/x-xcf";

            if (ExifTags.Count == 0) {
                Log.Debug ("Index: ", FullPath);
                ExifTags = lib.GetExifTags (fullPath: FullPath);
            }

            if (ExifTimestampCreated == null && ExifTimestampModified == null && ExifTimestampAcquired == null) {
                string fileName = Path.GetFileName (FullPath);
                DateTime date;
                if (NamingUtilities.GetFileNameDate (fileName: fileName, date: out date)) {
                    Log.Info ("Index: Set exif date for picture: ", FullPath, " => ", string.Format ("{0:yyyy:MM:dd HH:mm:ss}", date));
                    lib.SetExifDate (fullPath: FullPath, date: date);
                    ExifTags = lib.GetExifTags (fullPath: FullPath);
                    IsDateless = false;
                } else {
                    IsDateless = true;
                }
            } else {
                IsDateless = false;
            }

            if (IsCommonFormat && string.IsNullOrWhiteSpace (PixelHash.Hash)) {
                Log.Debug ("Index: ", FullPath);
                Bitmap bitmap = lib.ReadBitmap (fileName: FullPath);
                if (bitmap != null) {
                    using (bitmap) {
                        if (PixelHash.Hash == null) {
                            var result = lib.GetPixelHash (bitmap: bitmap);
                            if (result.HasValue) {
                                PixelHash = result.Value;
                            } else {
                                Log.Error ("Index: Unable to get pixel hash! fullPath=", FullPath);
                            }
                            Log.Info ("PixelHash: ", PixelHash);
                        }
                    }
                } else {
                    Log.Error ("Error! Index Picture: Can't read bitmap: ", FullPath);
                    IsDateless = true;
                }
            }
        }

        public override bool IsCompletelyIndexed {
            get {
                bool result = (ExifTags.Count > 0 || IsDateless)
                              && (!string.IsNullOrWhiteSpace (PixelHash.Hash) || !IsCommonFormat)
                              && !string.IsNullOrWhiteSpace (MimeType);

                if (!result)
                    Log.Debug ("Picture: IsCompletelyIndexed: (ExifTags.Count =", ExifTags.Count, " || IsDateless =", IsDateless, ") &&" +
                    " (!string.IsNullOrWhiteSpace (PixelHash.Hash) =", !string.IsNullOrWhiteSpace (PixelHash.Hash), " || !IsCommonFormat = ", !IsCommonFormat, ")" +
                    " && !string.IsNullOrWhiteSpace (MimeType) = ", !string.IsNullOrWhiteSpace (MimeType));

                return result;
            }
        }

        public static void RunIndexHooks (ref string fullPath)
        {
            // is the file ending in BMP format?
            if (Path.GetExtension (fullPath) == ".bmp") {
                Picture.ConvertToJpeg (fullPath: ref fullPath);
            }
            // is the file ending in TIF format?
            if (Path.GetExtension (fullPath) == ".tif" || Path.GetExtension (fullPath) == ".tiff") {
                Picture.ConvertToJpeg (fullPath: ref fullPath);
            }
            // WTF is pamp?
            if (Path.GetExtension (fullPath) == ".pamp") {
                Picture.ConvertToJpeg (fullPath: ref fullPath);
            }
            // As jpegs, Screenshots don't look any different and save much disk space
            if (Path.GetExtension (fullPath) == ".png") {
                if (Path.GetFileName (fullPath).ToLower ().Contains ("screenshot")) {
                    if (fullPath.ToLower ().Contains ("serie")) {
                        Picture.ConvertToJpeg (fullPath: ref fullPath, quality: 65);
                    } else {
                        Picture.ConvertToJpeg (fullPath: ref fullPath, quality: 95);
                    }
                }
            }
        }

        public static bool ConvertToJpeg (ref string fullPath, int quality = 95)
        {
            string oldPath = fullPath;
            string newPath = Path.GetDirectoryName (oldPath) + SystemInfo.PathSeparator + Path.GetFileNameWithoutExtension (oldPath) + ".jpg";

            try {
                Log.Info ("Convert picture to JPEG: ", Path.GetFileName (oldPath), " => ", Path.GetFileName (newPath), " (quality: ", quality, ")");
                Bitmap original = (Bitmap)Image.FromFile (oldPath);

                JpegHelper.Current.Save (image: original, filename: newPath, compression: new CompressionParameters { Quality = quality });

                lib.CopyExifTags (sourcePath: oldPath, destPath: newPath);
                if (File.Exists (newPath) && File.Exists (oldPath)) {
                    File.Delete (oldPath);
                    fullPath = newPath;
                }
                return true;
            } catch (NotImplementedException ex) {
                Log.Error ("NotImplementedException is thrown by BitMiracle.LibJpeg!");
                Log.Error (ex);
                try {
                    Log.Info ("Convert picture to JPEG (using the awful .NET encoder): ", Path.GetFileName (oldPath), " => ", Path.GetFileName (newPath), " (quality: ", quality, ")");
                    Bitmap original = (Bitmap)Image.FromFile (oldPath);

                    EncoderParameters encoderParams = new EncoderParameters (1);
                    encoderParams.Param [0] = new EncoderParameter (System.Drawing.Imaging.Encoder.Quality, 100L);
                    original.Save (filename: newPath, encoder: PictureLibrary.GetEncoder (ImageFormat.Jpeg), encoderParams: encoderParams);

                    lib.CopyExifTags (sourcePath: oldPath, destPath: newPath);
                    if (File.Exists (newPath) && File.Exists (oldPath)) {
                        File.Delete (oldPath);
                        fullPath = newPath;
                    }
                } catch (Exception ex2) {
                    Log.Error (ex2);
                }
            } catch (Exception ex) {
                Log.Error (ex);
            }
            return false;
        }

        protected override void SerializeInternal (Dictionary<string, string> dict)
        {
            // exif tags
            foreach (ExifTag tag in ExifTags) {
                string key;
                string value;
                if (tag.Serialize (out key, out value)) {
                    dict [key] = value;
                }
                if (key == "exif:UserComment" && value.Length > 100) {
                    dict [key] = "";
                }
            }

            // is dateless?
            dict ["flag:IsDateless"] = IsDateless ? "true" : "false";

            // is in a common format?
            dict ["flag:IsCommonFormat"] = IsCommonFormat ? "true" : "false";

            // save the pixel hash
            dict ["picture:PixelHash"] = PixelHash.Hash;
        }

        protected override void DeserializeInternal (Dictionary<string, string> dict)
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
            IsDateless = dict.ContainsKey ("flag:IsDateless") ? (dict ["flag:IsDateless"] == "true") : false;

            // is in a common format?
            IsCommonFormat = dict.ContainsKey ("flag:IsCommonFormat") ? (dict ["flag:IsCommonFormat"] == "true") : true;

            // load the pixel hash
            PixelHash = new HexString { Hash = dict ["picture:PixelHash"] };
        }
    }
}
