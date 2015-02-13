using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Media.Files;

namespace Shell.Media.Videos
{
    public class ActionCamVideo : ValueObject<ActionCamVideo>
    {
        public string FullPath { get; set; }

        public string FileName { get { return Path.GetFileName (FullPath); } }

        public DateTime Date { get; private set; }

        public ushort ConsecutiveNumber { get; private set; }

        public string FormattedDate { get { return Date.ToString (format: dateFormatSjcamVideo); } }

        public string FormattedConsecutiveNumber { get { return ConsecutiveNumber.ToString ("000", CultureInfo.InvariantCulture); } }

        private static Regex patternSjcamVideo = new Regex ("((?:19|20)[0-9]{2})_([0-9]{2})([0-9]{2})_([0-9]{6})_([0-9]{3})");

        private static string dateFormatSjcamVideo = "yyyy_MMdd_HHmmss";

        public ActionCamVideo (string fullPath)
        {
            FullPath = fullPath;

            // is the file name valid for a sjcam video file?
            if (!patternSjcamVideo.IsMatch (FileName)) {
                throw new ArgumentException ("ActionCamVideo: not a valid video file name: " + fullPath);
            }

            // get the date and time
            DateTime _date;
            if (NamingUtilities.GetFileNameDate (fileName: FileName, date: out _date)) {
                Date = _date;
            } else {
                string err = GetErrorMessage ("DateTime", FileName);
                Log.Error ("BUG!");
                Log.Error (err);
                throw new ArgumentException (err);
            }

            // get the consecutive number
            try {
                Match match = patternSjcamVideo.Match (FileName);
                if (match.Success) {
                    string num = match.Groups [5].Value;
                    ConsecutiveNumber = ushort.Parse (num);
                } else {
                    string err = GetErrorMessage ("consecutive number", FileName);
                    Log.Error (err);
                    throw new ArgumentException (err);
                }
            } catch (Exception ex) {
                string err = GetErrorMessage ("consecutive number", FileName);
                Log.Error ("BUG!");
                Log.Error (err);
                Log.Error (ex);
                throw new ArgumentException (err);
            }
        }

        private static string GetErrorMessage (string what, string path)
        {
            return "ActionCamVideo: Unable to determine the " + what + " of the following file: '" + path + "'";
        }

        public static bool IsValidVideoFile (string fullPath)
        {
            try {
                return new ActionCamVideo (fullPath: fullPath) != null;
            } catch (ArgumentException) {
                return false;
            } catch (IOException) {
                return false;
            }
        }


        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { FileName };
        }

        public override bool Equals (object obj)
        {
            return ValueObject<ActionCamVideo>.Equals (myself: this, obj: obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public static bool operator == (ActionCamVideo a, ActionCamVideo b)
        {
            return ValueObject<ActionCamVideo>.Equality (a, b);
        }

        public static bool operator != (ActionCamVideo a, ActionCamVideo b)
        {
            return ValueObject<ActionCamVideo>.Inequality (a, b);
        }
    }

}

