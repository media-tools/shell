using System;
using Shell.Common;
using System.IO;
using Shell.Common.Tasks;
using Shell.Common.IO;
using Shell.Common.Util;

namespace Shell.Logging
{
    public class LogTask : ScriptTask, MainScriptTask
    {
        public LogTask ()
        {
            Name = "Log";
            Description = "Print log file";
            Options = new string[] { "log" };
            ParameterSyntax = "[NUM_OF_LINES]";
        }

        protected override void InternalRun (string[] args)
        {
            string[] lines = null;
            Log.NeedAccessToLogfile = () => lines = File.ReadAllLines (Log.CURRENT_LOGFILE);

            int count = 100;
            bool tail = false;
            if (args.Length != 0) {
                if (args [0].EndsWith ("-") || args [0].EndsWith ("+")) {
                    args [0] = args [0].Substring (0, args [0].Length - 1);
                    tail = true;
                }
                try {
                    count = int.Parse (args [0]);
                } catch (Exception) {
                    Log.Error ("Invalid option: " + args [0]);
                    return;
                }
            }
            for (int i = Math.Max(lines.Length - count, 0); i < lines.Length; ++i) {
                printLine (lines [i]);
            }
            if (tail) {
                Log.NeedAccessToLogfile = tailLog;
            }
        }

        private void tailLog ()
        {
            using (StreamReader reader = new StreamReader(new FileStream(Log.CURRENT_LOGFILE, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
                //start at the end of the file
                long lastMaxOffset = reader.BaseStream.Length;

                while (true) {
                    System.Threading.Thread.Sleep (100);

                    //if the file size has not changed, idle
                    if (reader.BaseStream.Length == lastMaxOffset)
                        continue;

                    //seek to the last max offset
                    reader.BaseStream.Seek (lastMaxOffset, SeekOrigin.Begin);

                    //read out of the file until the EOF
                    string line = "";
                    while ((line = reader.ReadLine()) != null)
                        printLine (line);

                    //update the last max offset
                    lastMaxOffset = reader.BaseStream.Position;
                }
            }
        }

        private static string lastPid;

        void printLine (string str)
        {
            string[] parts = str.Split (new char[] { ' ' }, 4);
            if (parts.Length == 4) {
                if (parts [2] != lastPid && !string.IsNullOrEmpty(lastPid)) {
                    Console.WriteLine ();
                }
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write (">> ");
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write (parts [0] + " " + parts [1] + " ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write (StringUtils.Padding ((Commons.MAX_PID + "").Length - parts [2].Length) + parts [2] + " ");
                Console.ResetColor ();
                Console.WriteLine (parts [3]);
                Console.ResetColor ();
                lastPid = parts [2];
            }
        }
    }
}

