using System;

namespace Control.Common
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
    }
}

