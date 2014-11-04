using System;
using System.Linq;

namespace Shell.Common.Util
{
    public static class StringUtils
    {
        public static string JoinArgs (string[] args, string alt = "")
        {
            if (args.Length == 0) {
                return alt;
            } else {
                return "\"" + string.Join ("\" \"", args) + "\"";
            }
        }

        public static string Padding (int length)
        {
            return length <= 0 ? "" : String.Concat (Enumerable.Repeat (" ", length));
        }

        public static string SingleQuote (this string str)
        {
            return "'" + str.Replace ("'", "\\'") + "'";
        }

        public static string DoubleQuote (this string str)
        {
            return "\"" + str.Replace ("\"", "\\\"") + "\"";
        }

        public static string SingleQuoteShell (this string str)
        {
            return "'" + str.Replace ("'", "'\"'\"'") + "'";
        }

        public static string DoubleQuoteShell (this string str)
        {
            return "\"" + str.Replace ("\"", "\"'\"'\"") + "\"";
        }

        public static string RandomShareName ()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToLower ();
            var random = new Random ();
            var result = new string (Enumerable.Repeat (chars, 8).Select (s => s [random.Next (s.Length)]).ToArray ());
            return result;
        }
    }
}

