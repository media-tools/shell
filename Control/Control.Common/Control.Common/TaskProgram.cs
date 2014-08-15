using System;

namespace Control.Common
{
    public class TaskProgram
    {
        public Task[] Tasks { get; private set; }

        public TaskProgram (Task[] tasks)
        {
            Tasks = tasks;
        }

        public void Main (string[] args)
        {
            Log.Message (Commons.INFO_STR);
            Log.Debug ("Start (date='", Commons.DATETIME, "', args='", StringUtils.JoinArgs (args: args, alt: "(null)"), "')");

            Task matchingTask = null;
            if (args.Length > 0 && findMatchingTask (args, out matchingTask)) {
                matchingTask.Run (args);
            } else {
                printUsage ();
            }

            Log.Debug ("Stop (date='", Commons.DATETIME, "', runtime='", (int)Commons.RUNTIME_SEC, " sec')");
        }

        private void printUsage ()
        {
            Log.Message ("Tasks:");
            foreach (Task task in Tasks) {
                task.PrintUsage (indent: "  ");
            }
        }

        private bool findMatchingTask (string[] args, out Task matchingTask)
        {
            foreach (Task task in Tasks) {
                if (task.MatchesOption (args [0])) {
                    matchingTask = task;
                    return true;
                }
            }

            matchingTask = null;
            return false;
        }
    }
}

