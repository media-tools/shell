using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Shell.Common.IO;
using System.Collections.Generic;
using System.Threading;

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

        public static Platforms CurrentPlatform { get { return Environment.OSVersion.Platform.ToString ().StartsWith ("Win") ? Platforms.Windows : Platforms.Linux; } }

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

        public static int PendingOperations = 0;
        public static int CriticalOperations = 0;
        public static bool CanStartCriticalOperation = true;
        public static bool CanStartPendingOperation = true;
        public static List<Action> ExitHooks = new List<Action> ();

        public static void OnCancel ()
        {
            Log.MessageConsole ();
            Log.MessageLog ("Cancelled!");
            int i = 1;
            if (ExitHooks.Count > 0) {
                foreach (Action hook in ExitHooks) {
                    Log.MessageLog ("Running exit hook #", i, " of ", ExitHooks.Count, "...");
                    try {
                        hook ();
                    } catch (Exception ex) {
                        Log.MessageLog ("Fuck you! Exception in exit hook: ", ex);
                    }
                    Log.MessageLog ("Done running exit hook #", i, ".");
                    i++;
                }
            } else {
                Log.MessageLog ("There are no exit hooks.");
            }
            Log.MessageLog ("Exit.");
        }

        public static void CancelThreadWorker ()
        {
            CanStartPendingOperation = false;
            int timeout = 5000;
            while (PendingOperations > 0 && timeout > 0) {
                Thread.Sleep (100);
                timeout -= 100;
                Log.MessageLog ("Waiting for pending operations to finish (", timeout, " ms)...");
            }
            CanStartCriticalOperation = false;
            timeout = 30000;
            while (CriticalOperations > 0 && timeout > 0) {
                Thread.Sleep (100);
                timeout -= 100;
                Log.MessageLog ("Waiting for critical operations to finish (", timeout, " ms)...");
            }
            OnCancel ();
            Console.CursorVisible = true;
            Environment.Exit (0);
        }
    }

    public enum Platforms
    {
        Windows,
        Linux
    }
}

