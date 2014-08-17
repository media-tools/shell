using System;
using System.Collections.Generic;
using System.Linq;

namespace Control.Common
{
    public class Program
    {
        public Task[] Tasks { get; private set; }

        public static Action<Task> HooksBeforeTask = (tsk) => {};
        public static Action<Task> HooksAfterTask = (tsk) => {};

        public Program (Task[] tasks)
        {
            Tasks = tasks;
        }

        public void Main (string[] args)
        {
            Log.Message (Commons.INFO_STR);

            Task matchingTask = null;
            if (args.Length > 0 && findMatchingTask (args[0], out matchingTask)) {
                Log.Debug ("Start (date='", Commons.DATETIME, "', args='", StringUtils.JoinArgs (args: args, alt: "(null)"), "')");

                HooksBeforeTask (matchingTask);
                matchingTask.Run (args.Skip(1).ToArray());
                HooksAfterTask (matchingTask);

                Log.Debug ("Stop (date='", Commons.DATETIME, "', runtime='", (int)Commons.RUNTIME_SEC, " sec')");
            } else {
                printUsage ();
            }
        }

        private void printUsage ()
        {
            Console.WriteLine ("Usage: " + Commons.EXE_PATH + " [TASK] [OPTIONS]");
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

