using System;
using System.IO;

namespace Control.Common
{
    public static class Commons
    {
        public static int VERSION = 1;

        public static string VERSION_STR { get { return "0." + VERSION + ""; } }

        public static string EXE_NAME { get { return Path.GetFileNameWithoutExtension (System.Reflection.Assembly.GetEntryAssembly ().Location); } }

        public static string EXE_PATH { get { return System.Reflection.Assembly.GetEntryAssembly ().Location; } }

        public static string INFO_STR { get { return EXE_NAME + " " + VERSION_STR + " (C) 2014 Tobias Schulz"; } }

        public static string DATE { get { return DateTime.Now.ToString ("dd.MM.yyyy"); } }

        public static string TIME { get { return DateTime.Now.ToString ("HH:mm:ss"); } }

        public static string DATETIME { get { return DateTime.Now.ToString ("dd.MM.yyyy HH:mm:ss"); } }

        public static DateTime StartedAt;

        public static double RUNTIME_SEC { get { return (DateTime.Now - StartedAt).TotalSeconds; } }

        static Commons ()
        {
            StartedAt = DateTime.Now;
        }
    }
}

