using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;

namespace Shell.Common
{
    public sealed class Program
    {
        public static ScriptTask[] Tasks { get; private set; }

        public static Action<ScriptTask> HooksBeforeTask = (tsk) => {
        };
        public static Action<ScriptTask> HooksAfterTask = (tsk) => {
        };

        public Program (IEnumerable<ScriptTask> tasks)
        {
            Tasks = tasks.ToArray ();
        }

        public void Main (string[] args)
        {
            // check for experimental flag
            Commons.IS_EXPERIMENTAL = args.Where (arg => arg.StartsWith ("--ex")).Any ();
            args = (from arg in args
                             where !arg.StartsWith ("--ex")
                             select arg).ToArray ();

            ScriptTask matchingTask = null;
            if (args.Length > 0 && findMatchingTask (args [0], out matchingTask)) {
                Log.MessageLog ("Start (date='", Commons.DATETIME, "', args='", StringUtils.JoinArgs (args: args, alt: "(null)"), "')");

                if (Commons.IS_EXPERIMENTAL) {
                    Log.Message (LogColor.DarkBlue, "=== Experimental mode! ===", LogColor.Reset);
                }

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

            string indent = "  ";
            if (Tasks.Any ()) {
                int maxOptionLength = Tasks.Max (task => task.LengthOfOption (indent: indent));
                int maxLineLength = Tasks.Max (task => task.LengthOfUsageLine (indent: indent, maxOptionLength: maxOptionLength));
                foreach (ScriptTask task in Tasks) {
                    task.PrintUsage (indent: indent, maxLineLength: maxLineLength, maxOptionLength: maxOptionLength);
                }
            } else {
                Console.WriteLine ("No tasks found.");
            }
            Console.WriteLine ("");
        }

        private bool findMatchingTask (string arg, out ScriptTask matchingTask)
        {
            foreach (ScriptTask task in Tasks) {
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

