using System;
using System.Linq;

namespace Control.Common.Util
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
            return String.Concat (Enumerable.Repeat (" ", length));
        }
    }
}

