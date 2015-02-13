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
    public sealed class PictureLibrary : Library
    {
        private static SHA256Managed crypt = new SHA256Managed ();


        public PictureLibrary ()
        {
            ConfigName = NamespaceMedia.CONFIG_NAME;
        }

        public List<ExifTag> GetExifTags (string fullPath)
        {
            //string script = "exiftool -time:all -a -G0:1 -s " + fullPath.SingleQuoteShell ();
            string script = "exiftool -a -G0:1 -s " + fullPath.SingleQuoteShell ();

            List<ExifTag> tags = new List<ExifTag> ();
            Action<string> receiveOutput = line => {
                // example: [EXIF:ExifIFD]  DateTimeOriginal                : 2014:10:23 22:45:11

                if (!line.Contains ("ICC_Profile")) {
                    ExifTag tag;
                    if (ExifTag.ReadFromExiftoolConsoleOutput (line, out tag)) {
                        tags.Add (tag);
                        Log.Message ("- ", Log.Fill (tag.Name, 30), ": ", tag.Value);
                    }
                }
            };

            fs.Runtime.WriteAllText (path: "run1.sh", contents: script);


            Log.Message ("EXIF tags:");
            Log.Indent++;
            fs.Runtime.ExecuteScript (path: "run1.sh", receiveOutput: receiveOutput, ignoreEmptyLines: true, verbose: false);
            Log.Indent--;

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

        public Bitmap ReadBitmap (string fileName)
        {
            try {
                using (Stream stream = File.Open (path: fileName, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.ReadWrite)) {
                    Bitmap bitmap = (Bitmap)Image.FromStream (stream: stream, useEmbeddedColorManagement: true, validateImageData: false);
                    Log.Debug ("ReadBitmap: FromStream: bitmap=", bitmap);
                    return bitmap;
                }
            } catch (Exception) {
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

}
