using System;
using System.Collections.Generic;
using System.Linq;
using Control.Common.IO;
using Control.Common.Tasks;
using Control.Common.Util;

namespace Control.Common
{
    public class Program
    {
        public static Task[] Tasks { get; private set; }

        public static Action<Task> HooksBeforeTask = (tsk) => {};
        public static Action<Task> HooksAfterTask = (tsk) => {};

        public Program (IEnumerable<Task> tasks)
        {
            Tasks = tasks.ToArray ();
        }

        public void Main (string[] args)
        {
            Task matchingTask = null;
            if (args.Length > 0 && findMatchingTask (args [0], out matchingTask)) {
                Log.MessageLog ("Start (date='", Commons.DATETIME, "', args='", StringUtils.JoinArgs (args: args, alt: "(null)"), "')");

                HooksBeforeTask (matchingTask);
                matchingTask.Run (args.Skip (1).ToArray ());
                HooksAfterTask (matchingTask);

                Log.MessageLog ("Stop (date='", Commons.DATETIME, "', runtime='", (int)Commons.RUNTIME_SEC, " sec')");
            } else {
                printUsage ();
            }
        }

        private void printUsage ()
        {
            Log.MessageLog ("");
            Log.Message (Commons.INFO_STR);

            Log.MessageLog ("Print usage...");
            Console.ResetColor ();
            Console.Write ("Usage: ");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write (Commons.EXE_PATH);

            Console.ResetColor ();
            Console.Write (" ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write ("[TASK]");
            Console.ResetColor ();
            Console.Write (" ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine ("[OPTIONS]");
            Console.WriteLine ("");
            Console.WriteLine ("Tasks:");
            foreach (Task task in Tasks) {
                task.PrintUsage (indent: "  ");
            }
            Console.WriteLine ("");
        }

        private bool findMatchingTask (string arg, out Task matchingTask)
        {
            foreach (Task task in Tasks) {
                if (task.MatchesOption (arg)) {
                    matchingTask = task;
                    return true;
                }
            }

            matchingTask = null;
            return false;
        }
    }
}

