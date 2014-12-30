using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Namespaces;
using Shell.Media.Files;

namespace Shell.Media.Content
{
    public sealed class PictureLibrary : Library
    {
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
                if (ExifTag.ReadFromExiftool (line, out tag)) {
                    tags.Add (tag);
                }
            };

            fs.Runtime.WriteAllText (path: "run1.sh", contents: script);
            fs.Runtime.ExecuteScript (path: "run1.sh", receiveOutput: receiveOutput, ignoreEmptyLines: true);

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
                string regexFormat = format.Replace ("yyyy", "(19|20)[0-9][0-9]").Replace ("MM", "[0-9][0-9]").Replace ("dd", "[0-9][0-9]").Replace ("HH", "[0-9][0-9]")
                    .Replace ("mm", "[0-9][0-9]").Replace ("ss", "[0-9][0-9]").Replace (".", "\\.");
                string regex = "^" + regexPrefix + regexFormat;
                // Log.Debug ("prefix=", prefix, ", format=", format, ", regex=", regex);
                if (Regex.IsMatch (fileName, regex)) {
                    try {
                        date = DateTime.ParseExact (fileName.Substring (prefix.Length, format.Length), format,
                            System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    } catch (Exception e) {
                        Log.Error (e);
                    }
                }
            }
            date = new DateTime ();
            return false;
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

        public static bool ReadFromExiftool (string line, out ExifTag tag)
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
