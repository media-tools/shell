using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ExifUtils.Exif;
using ExifUtils.Exif.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Media.Files;
using Shell.Namespaces;

namespace Shell.Media.Content
{
    public sealed class PictureLibrary : Library
    {
        private static SHA256Managed crypt = new SHA256Managed ();


        public PictureLibrary ()
        {
            ConfigName = NamespacePictures.CONFIG_NAME;
        }

        public List<ExifTag> GetExifTags (string fullPath)
        {
            string script = "exiftool -time:all -a -G0:1 -s " + fullPath.SingleQuoteShell ();

            List<ExifTag> tags = new List<ExifTag> ();
            Action<string> receiveOutput = line => {
                // example: [EXIF:ExifIFD]  DateTimeOriginal                : 2014:10:23 22:45:11

                ExifTag tag;
                if (ExifTag.ReadFromExiftoolConsoleOutput (line, out tag)) {
                    tags.Add (tag);
                }
            };

            fs.Runtime.WriteAllText (path: "run1.sh", contents: script);
            fs.Runtime.ExecuteScript (path: "run1.sh", receiveOutput: receiveOutput, ignoreEmptyLines: true);

            return tags;
        }

        public List<ExifTag> GetExifTags (Bitmap bitmap)
        {
            List<ExifTag> tags = new List<ExifTag> ();


            ExifPropertyCollection exifProperties = ExifReader.GetExifData (bitmap: bitmap);
            foreach (ExifProperty property in exifProperties) {
                ExifTag tag;
                if (ExifTag.FromExifProperty (property, out tag)) {
                    Log.Debug ("property: Tag=", tag.Name, ", Value=", tag.Value);

                    tags.Add (tag);
                }
            }

            return tags;
        }

        public void SetExifDate (string fullPath, DateTime date)
        {
            string dateString = string.Format ("{0:yyyy:MM:dd HH:mm:ss}", date); //JJJJ:MM:TT HH:MM:SS
            string script = "exiftool -AllDates='" + dateString + "' '" + fullPath + "' && rm -f " + fullPath.SingleQuoteShell () + "_original";

            fs.Runtime.WriteAllText (path: "run2.sh", contents: script);
            fs.Runtime.ExecuteScript (path: "run2.sh", ignoreEmptyLines: true);
        }

        public void CopyExifTags (string sourcePath, string destPath)
        {
            string script = "exiftool -TagsFromFile " + sourcePath.SingleQuoteShell () + " " + destPath.SingleQuoteShell ()
                            + " && rm -f " + destPath.SingleQuoteShell () + "_original ";

            fs.Runtime.WriteAllText (path: "run3.sh", contents: script);
            fs.Runtime.ExecuteScript (path: "run3.sh", ignoreEmptyLines: true, verbose: false);
        }

        class FormatCombo
        {
            public string Prefix;
            public string Format;
        }

        FormatCombo[] dateFormats = new FormatCombo[] {
            new FormatCombo { Prefix = "", Format = "yyyyMMdd_HHmmss" },
            new FormatCombo { Prefix = "", Format = "yyyyMMdd-HHmmss" },
            new FormatCombo { Prefix = "", Format = "yyyyMMdd HHmmss" },
            new FormatCombo { Prefix = "", Format = "yyyyMMdd" },
            new FormatCombo { Prefix = "IMG_", Format = "yyyyMMdd_HHmmss" },
            new FormatCombo { Prefix = "IMG_", Format = "ddMMyyyy_HHmmss" },
            new FormatCombo { Prefix = "IMG-", Format = "yyyyMMdd-HHmmss" },
            new FormatCombo { Prefix = "IMG-", Format = "yyyyMMdd" },
            new FormatCombo { Prefix = "Screenshot_", Format = "yyyy-MM-dd-HH-mm-ss" },
            new FormatCombo { Prefix = "Screenshot_", Format = "yyyy-MM-dd HH.mm.ss" },
            new FormatCombo { Prefix = "Screenshot ", Format = "yyyy-MM-dd HH.mm.ss" },
            new FormatCombo { Prefix = "Screenshot - ", Format = "dd.MM.yyyy - HH_mm_ss" },
            new FormatCombo { Prefix = "Bildschirmfoto vom ", Format = "yyyy-MM-dd HH_mm_ss" },
            new FormatCombo { Prefix = "Bildschirmfoto - ", Format = "dd.MM.yyyy - HH_mm_ss" },
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd HH-mm-ss" },
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd HH.mm.ss" },
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd HHmmss" },
            
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd-HH-mm-ss" },
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd-HH.mm.ss" },
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd-HHmmss" },
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd_HH-mm-ss" },
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd_HH.mm.ss" },
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd_HHmmss" },
            new FormatCombo { Prefix = "", Format = "yyyy-MM-dd" },
        };

        public bool GetFileNameDate (string fileName, out DateTime date)
        {
            foreach (FormatCombo formatCombo in dateFormats) {
                string prefix = formatCombo.Prefix;
                string format = formatCombo.Format;
                string regexPrefix = prefix;
                string regexFormat = format.Replace ("yyyy", "(?:19|20)[0-9][0-9]").Replace ("MM", "[0-9][0-9]").Replace ("dd", "[0-9][0-9]").Replace ("HH", "[0-9][0-9]")
                    .Replace ("mm", "[0-9][0-9]").Replace ("ss", "[0-9][0-9]").Replace (".", "\\.");

                foreach (string preprefix in new [] { "^", "" }) {
                    Regex regex = new Regex (preprefix + regexPrefix + "(" + regexFormat + ")");
                    // Log.Debug ("prefix=", prefix, ", format=", format, ", regex=", regex);
                    Match match = regex.Match (fileName);
                    if (match.Success) {
                        string dateTimeStr = match.Groups [1].Value;
                        Log.Debug ("GetFileNameDate: fileName=", fileName, ", dateTimeStr=", dateTimeStr);
                        try {
                            date = DateTime.ParseExact (dateTimeStr, format,
                                System.Globalization.CultureInfo.InvariantCulture);
                            return true;
                        } catch (Exception e) {
                            Log.Error (e);
                        }
                    }
                }
            }
            date = new DateTime ();
            return false;
        }


        public Bitmap ReadBitmap (string fileName)
        {
            try {
                using (Stream stream = File.Open (path: fileName, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.ReadWrite)) {
                    Bitmap bitmap = (Bitmap)Image.FromStream (stream: stream, useEmbeddedColorManagement: true, validateImageData: false);
                    Log.Debug ("ReadBitmap: FromStream: bitmap=", bitmap);
                    return bitmap;
                }
            } catch (Exception ex) {
                try {
                    Bitmap bitmap = (Bitmap)Image.FromFile (fileName);
                    Log.Debug ("ReadBitmap: FromFile: bitmap=", bitmap);
                    return bitmap;
                } catch (Exception ex2) {
                    Log.Error (ex2);
                    return null;
                }
            }
        }

        public HexString? GetPixelHash (string fileName)
        {
            Bitmap bitmap = ReadBitmap (fileName);
            if (bitmap != null) {
                using (bitmap) {
                    return GetPixelHash (bitmap: bitmap);
                }
            } else {
                return null;
            }
        }

        public HexString? GetPixelHash (Bitmap bitmap)
        {
            try {
                byte[] bytes = Array1DFromBitmap (bitmap);
                return HexString.FromByteArray (crypt.ComputeHash (bytes));
            } catch (Exception ex) {
                Log.Error (ex);
                return null;
            }
        }

        public byte[] Array1DFromBitmap (Bitmap bmp)
        {
            if (bmp == null)
                throw new NullReferenceException ("Bitmap is null");

            Rectangle rect = new Rectangle (0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits (rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            IntPtr ptr = data.Scan0;

            //declare an array to hold the bytes of the bitmap
            int numBytes = data.Stride * bmp.Height;
            byte[] bytes = new byte[numBytes];

            //copy the RGB values into the array
            System.Runtime.InteropServices.Marshal.Copy (ptr, bytes, 0, numBytes);

            bmp.UnlockBits (data);

            return bytes;           
        }

        public static ImageCodecInfo GetEncoder (ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders ();
            foreach (ImageCodecInfo codec in codecs) {
                if (codec.FormatID == format.Guid) {
                    return codec;
                }
            }
            return null;
        }

        public DateTime? TryParseExifTimestamp (List<ExifTag> exifTags, string[] possibleTagNames)
        {
            string lastDateTimeStr = null;
            foreach (string possibleTagName in possibleTagNames) {
                if (exifTags.Any (tag => tag.Name == possibleTagName)) {
                    string dateTimeStr = exifTags.First (tag => tag.Name == possibleTagName).Value;
                    if (dateTimeStr.Length >= 8) {
                        lastDateTimeStr = dateTimeStr;

                        DateTime? result = null;
                        DateTime dt;
                        dateTimeStr = dateTimeStr.TrimEnd ('Z');
                        string[] possibleDateFormats = new [] {
                            "yyyy:MM:dd HH:mm:ss", "yyyy:MM:dd HH:mm:sszzz", "yyyy:MM:dd HH:mm:sszz"
                        };
                        foreach (string possibleDateFormat in possibleDateFormats) {
                            if (DateTime.TryParseExact (dateTimeStr, possibleDateFormat, 
                                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt)) {
                                result = dt;
                                break;
                            }
                        }
                        if (DateTime.TryParse (dateTimeStr, out dt)) {
                            result = dt;
                        }

                        if (result.HasValue) {
                            return result.Value;
                        }
                    }
                }
            }
            if (lastDateTimeStr != null) {
                Log.Message ("Error parsing exif datetime: ", lastDateTimeStr);
            } else if (exifTags.Count > 0) {
                Log.Message ("No ", string.Join (" or ", possibleTagNames), ", but: ", string.Join ("; ", exifTags.Select (tag => tag.Name + "=" + tag.Value)));
            }
            return null;
        }
    }

    public sealed class ExifTag
    {
        public string Name;
        public string Value;

        private ExifTag (string name, string value)
        {
            Name = name;
            Value = value;
        }

        public static bool FromExifProperty (ExifProperty property, out ExifTag tag)
        {
            tag = new ExifTag (property.Tag.ToString (), property.Value.ToString ());
            return true;
        }

        public static bool ReadFromExiftoolConsoleOutput (string line, out ExifTag tag)
        {
            tag = null;
            string[] parts = line.Split (new []{ ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2) {
                string value = parts [1].Trim ();
                string[] parts2 = parts [0].Trim ().Split (new char[]{ ']' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts2.Length == 2) {
                    string key = parts2 [1].Trim ();
                    string type = parts2 [0].Trim ().Trim ('[');
                    if (type != "File:System") {
                        tag = new ExifTag (key, value);
                    }
                } else {
                    Log.Debug ("Invalid key in exiftool output line: ", parts [0].Trim ());
                }
            } else {
                Log.Debug ("Invalid exiftool output line: ", line);
            }
            return tag != null;
        }

        public bool Serialize (out string key, out string value)
        {
            key = "exif:" + Name;
            value = Value;
            return true;
        }

        public static bool Deserialize (string key, string value, out ExifTag deserialized)
        {
            if (key.StartsWith ("exif:")) {
                deserialized = new ExifTag (name: key.Substring (5), value: value);
                return true;
            } else {
                deserialized = null;
                return false;
            }
        }
    }
}
