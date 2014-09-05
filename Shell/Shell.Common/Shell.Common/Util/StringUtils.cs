using System;
using System.Linq;

namespace Shell.Common.Util
{
    public class StringUtils
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
    }
}

