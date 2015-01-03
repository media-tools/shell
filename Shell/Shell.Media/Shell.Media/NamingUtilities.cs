using System;
using System.Text.RegularExpressions;

namespace Shell.Media
{
    public static class NamingUtilities
    {
        private static string patternGermanDateFormat = "(^|[^0-9])([0-9]{2})[.]([0-9]{2})[.]((?:19|20)[0-9]{2})([^0-9]|$)";

        public static bool IsNormalizedAlbumName (string name)
        {
            if (name.Contains ("Kamera 20")) {
                return false;
            } else if (name.Length == 0) {
                return false;
            } else if (Regex.IsMatch (name, patternGermanDateFormat)) {
                return false;
            } else {
                return true;
            }
        }

        public static string NormalizeAlbumName (string name)
        {
            bool done;
            do {
                if (name.Contains ("Kamera 20")) {
                    name = Regex.Replace (name, "Kamera", "Auto Backup");
                    done = false;
                } else if (name.Length == 0) {
                    name = "Unknown";
                    done = false;
                } else if (Regex.IsMatch (name, patternGermanDateFormat)) {
                    name = Regex.Replace (name, patternGermanDateFormat, "$1$4-$3-$2$5");
                    done = false;
                } else {
                    done = true;
                }
            } while (!done);
            return name;
        }

        private static string patternDate = "((?:19|20)[0-9]{2})([0-1][0-9])([0-3][0-9])";
        private static string patternTime = "([0-2][0-9])([0-6][0-9])([0-6][0-9])";
        private static string patternShortUserName = "([a-z]+)";

        public static bool IsPreferredFileName (string fileName)
        {
            return Regex.IsMatch (fileName, "^" + patternShortUserName + "_" + patternDate + "_" + patternTime);
        }
    }
}

