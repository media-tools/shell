using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Media.Files;
using Shell.Namespaces;

namespace Shell.Media.Pictures
{
    public sealed class ExifTag
    {
        public string Name;
        public string Value;

        private ExifTag (string name, string value)
        {
            Name = name;
            Value = value;
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

        public string Serialize ()
        {
            return "exif:" + Name + ":=" + Value;
        }

        public static bool Deserialize (string keyAndValue, out ExifTag deserialized)
        {
            string[] splittedLine = keyAndValue.Split (new [] { ":=" }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (splittedLine.Length == 2) {
                return Deserialize (splittedLine [0], splittedLine [1], out deserialized);
            } else {
                deserialized = null;
                return false;
            }
        }
    }

    public static class ExifTagExtensions
    {
        public static string Serialize (List<ExifTag> tags)
        {
            return string.Join ("\n", tags.Select (tag => tag.Serialize ()));
        }

        public static List<ExifTag> Deserialize (string serialized)
        {
            return serialized.Split (new [] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select (line => {
                ExifTag tag;
                ExifTag.Deserialize (keyAndValue: line, deserialized: out tag);
                return tag;
            }).Where (tag => tag != null).ToList ();
        }
    }
}
