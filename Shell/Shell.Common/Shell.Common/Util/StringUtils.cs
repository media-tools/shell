using System;
using System.Linq;
using System.Text.RegularExpressions;

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

        public static bool Contains (this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf (toCheck, comp) >= 0;
        }

        private static Regex regexRemoveNonLetters = new Regex ("[^a-zA-Z]");
        private static Regex regexRemoveNonDigits = new Regex ("[^0-9]");

        public static int CountLetters (this string text)
        {
            text = regexRemoveNonLetters.Replace (text, "");
            foreach (string meaninglessWord in meaninglessWords) {
                text = text.Replace (meaninglessWord, "");
            }
            return text.Length;
        }

        private static string[] meaninglessWords = new [] {
            "AllerleiThomasHeinz", "Screenshot", "Bildschirmfoto", "IMG", "MVC"
        };

        public static int CountDigits (this string text)
        {
            return regexRemoveNonDigits.Replace (text, "").Length;
        }

        public static string OnlyLetters (this string text)
        {
            text = regexRemoveNonLetters.Replace (text, "");
            return text;
        }

        public static string OnlyDigits (this string text)
        {
            text = regexRemoveNonDigits.Replace (text, "");
            return text;
        }
    }
}

