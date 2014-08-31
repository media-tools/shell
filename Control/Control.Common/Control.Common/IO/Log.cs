using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Control.Common.Util;
using System.Linq;

namespace Control.Common.IO
{
    public static class Log
    {
        public static string CURRENT_LOGFILE { get; private set; }

        private static StreamWriter logFile;

        public static void Init (string program = "game", string version = "")
        {
            try {
                string filename = (string.IsNullOrWhiteSpace (version) ? program : program + "-" + version) + ".log";
                CURRENT_LOGFILE = SystemInfo.LogDirectory + SystemInfo.PathSeparator + filename;
                logFile = File.AppendText (CURRENT_LOGFILE);
            } catch (Exception ex) {
                // we don't give a fuck whether we can open a log file
                logFile = StreamWriter.Null;
                Console.WriteLine (ex.ToString ());
            }
        }

        public static Action NeedAccessToLogfile {
            set {
                logFile.Close ();
                value ();
                logFile = File.AppendText (CURRENT_LOGFILE);
            }
        }

        private static bool isNewLineInLogfile = true;

        private static void LogFileWrite (params object[] message)
        {
            string text = string.Join ("", NoNull (NoColors (message)));
            if (isNewLineInLogfile) {
                LogFilePrefix ();
                logFile.Write (IndentString);
            }
            try {
                logFile.Write (text);
                isNewLineInLogfile = false;
            } catch (Exception) {
            }
        }

        private static int k = 0;

        private static void LogFileWriteLine (params object[] message)
        {
            string text = string.Join ("", NoNull (NoColors (message)));
            if (isNewLineInLogfile) {
                LogFilePrefix ();
                logFile.Write (IndentString);
            }
            try {
                logFile.WriteLine (text);
                isNewLineInLogfile = true;
                if (k % 100 == 0) {
                    logFile.Flush ();
                }
            } catch (Exception) {
            }
        }

        private static void LogFilePrefix ()
        {
            try {

                logFile.Write (Commons.DATETIME_LOG + " " + Commons.PID + " ");
            } catch (Exception) {
            }
        }
        // Indent
        public static byte Indent {
            get {
                return _indent;
            }
            set {
                if (value >= 0 && value <= 10) {
                    _indent = value;
                }
            }
        }

        public static readonly int IndentWidth = 2;
        private static byte _indent = 0;

        public static string IndentString { get { return String.Concat (Enumerable.Repeat (" ", (int)Indent * IndentWidth)); } }

        public static void Debug (params object[] message)
        {
            DebugConsole (message);
            DebugLog (message);
        }

        public static void DebugLog (params object[] message)
        {
            string str = string.Join ("", NoNull (message));
            LogFileWriteLine (str);
        }

        public static void DebugConsole (params object[] message)
        {
            printConsole (new object[] { LogColor.DarkGray }.Concat (message));
        }

        public static void Message (params object[] message)
        {
            MessageConsole (message);
            MessageLog (message);
        }

        public static void MessageLog (params object[] message)
        {
            LogFileWriteLine (message);
        }

        public static void MessageConsole (params object[] message)
        {
            printConsole (message);
        }

        private static void printConsole (IEnumerable<object> message)
        {
            Console.ResetColor ();
            Console.Write (IndentString);
            // if there are any colored objects in the message
            if (message.OfType<LogColor> ().Any ()) {
                foreach (object obj in NoNull (message)) {
                    if (obj is ControlCharacters && (ControlCharacters)obj == ControlCharacters.Newline) {
                        Console.WriteLine ();
                        Console.Write (IndentString);
                    } else if (obj is LogColor) {
                        if ((LogColor)obj == LogColor.Reset) {
                            Console.ResetColor ();
                        } else {
                            Console.ForegroundColor = ((LogColor)obj).ToConsoleColor ();
                        }
                    } else {
                        Console.Write (obj.ToString ());
                    }
                }
                Console.WriteLine ();
            }
            // faster method if nothing has a specific color
            else {
                string str = string.Join ("", NoNull (message));
                Console.WriteLine (str);
            }
            Console.ResetColor ();
        }

        public static void Error (params object[] message)
        {
            string str = string.Join ("", NoNull (NoColors (message)));
            printConsole (new object[] { LogColor.DarkRed, str });
            LogFileWriteLine (str);
            logFile.Flush ();
        }

        private static IEnumerable<object> NoNull (IEnumerable<object> message)
        {
            return from obj in message
                            select obj != null ? obj : "null";
        }

        private static IEnumerable<object> NoColors (IEnumerable<object> message)
        {
            return from obj in message
                            select obj is LogColor ? "" : obj;
        }

        public static ProgressBar OpenProgressBar (string identifier, string description)
        {
            return new ProgressBar (identifier: identifier, description: description);
        }
    }
}
