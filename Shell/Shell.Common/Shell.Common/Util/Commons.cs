using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Shell.Common.IO;

namespace Shell.Common.Util
{
    public static class Commons
    {
        public static int VERSION = 2;

        public static string VERSION_STR { get { return "0." + VERSION + ""; } }

        public static string EXE_PATH { get { return System.Reflection.Assembly.GetEntryAssembly ().Location; } }

        public static string EXE_NAME { get { return Path.GetFileNameWithoutExtension (System.Reflection.Assembly.GetEntryAssembly ().Location); } }

        public static string EXE_FILENAME { get { return Path.GetFileName (System.Reflection.Assembly.GetEntryAssembly ().Location); } }

        public static string EXE_DIRECTORY { get { return Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ().Location); } }

        public static string INFO_STR { get { return EXE_NAME + " " + VERSION_STR + " (C) 2014 Tobias Schulz"; } }

        public static string DATE { get { return DateTime.Now.ToString ("dd.MM.yyyy"); } }

        public static string TIME { get { return DateTime.Now.ToString ("HH:mm:ss"); } }

        public static string DATETIME { get { return DateTime.Now.ToString ("dd.MM.yyyy HH:mm:ss"); } }

        public static string DATETIME_LOG { get { return DateTime.Now.ToString ("yyyy.MM.dd HH:mm:ss"); } }

        public static DateTime StartedAt;

        public static double RUNTIME_SEC { get { return (DateTime.Now - StartedAt).TotalSeconds; } }

        public static int PID { get; private set; }

        public static int MAX_PID { get; private set; }

        public static bool IS_EXPERIMENTAL = false; 

        static Commons ()
        {
            StartedAt = DateTime.Now;
            PID = Process.GetCurrentProcess ().Id;

            try {
                MAX_PID = int.Parse (File.ReadAllText ("/proc/sys/kernel/pid_max"));
            } catch (Exception ex) {
                MAX_PID = (int)Math.Pow (2, 22);
                Console.WriteLine (ex);
            }
        }

        public static void OnCancel ()
        {
            Log.MessageConsole ();
            Log.MessageLog ("Cancelled!");
        }
    }
}

