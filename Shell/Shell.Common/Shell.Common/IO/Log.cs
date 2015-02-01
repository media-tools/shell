using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Shell.Common.Util;

namespace Shell.Common.IO
{
    public static class Log
    {
        public static bool DEBUG_ENABLED = false;

        public static string CURRENT_LOGFILE { get; private set; }

        private static StreamWriter logFile;

        public static int MAX_WIDTH = 150;

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
            string text = string.Join ("", NoNull (NoColors (NoShellCharacters (message))));
            if (isNewLineInLogfile) {
                logFile.Write (LogFilePrefix + IndentString);
            }
            text = text.Replace ("\n", "\n" + LogFilePrefix + IndentString);
            try {
                logFile.Write (text);
                isNewLineInLogfile = false;
            } catch (Exception) {
            }
        }

        private static int k = 0;

        private static void LogFileWriteLine (params object[] message)
        {
            string text = string.Join ("", NoNull (NoColors (NoShellCharacters (message))));
            if (isNewLineInLogfile) {
                logFile.Write (LogFilePrefix + IndentString);
            }
            text = text.Replace ("\n", "\n" + LogFilePrefix + IndentString);
            try {
                logFile.WriteLine (text);
                isNewLineInLogfile = true;
                if (k % 100 == 0) {
                    logFile.Flush ();
                }
            } catch (Exception) {
            }
        }

        public static string FormatString (params object[] message)
        {
            return string.Join ("", NoNull (NoColors (NoShellCharacters (message))));
        }

        // Prefix
        private static string LogFilePrefix { get { return Commons.DATETIME_LOG + " " + Commons.PID + " "; } }

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

        // Indent safety for try {} catch {} finally {}

        private static List<byte> previousIndentsTryFinally = new List<byte> ();

        public static void Try ()
        {
            previousIndentsTryFinally.Add (Indent);
        }

        public static void Finally ()
        {
            if (previousIndentsTryFinally.Count > 0) {
                int indentNo = previousIndentsTryFinally.Count - 1;
                Indent = previousIndentsTryFinally [indentNo];
                previousIndentsTryFinally.RemoveAt (indentNo);
            }
        }

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
            if (DEBUG_ENABLED) {
                printConsole (new object[] { LogColor.DarkGray }.Concat (message));
            }
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

        public static bool IsIncompleteLine = false;
        public static int IncompleteLineMaxWidth = 0;

        private static void printConsole (IEnumerable<object> message)
        {
            if (IsIncompleteLine) {
                Console.ResetColor ();
                //Console.WriteLine ();
                int left = Console.CursorLeft;
                Console.Write (String.Concat (Enumerable.Repeat (" ", Math.Max (0, IncompleteLineMaxWidth))));
                Console.CursorLeft = left;
                Console.Out.Flush ();
                IsIncompleteLine = false;
            }
            Console.ResetColor ();
            Console.Write (IndentString);
            // if there are any colored objects in the message
            if (message.OfType<LogColor> ().Any ()) {
                foreach (object obj in NoNull (message)) {
                    if (obj is ShellCharacters && (ShellCharacters)obj == ShellCharacters.Newline) {
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

        private static IEnumerable<object> NoShellCharacters (IEnumerable<object> message)
        {
            return from obj in message
                            select obj is ShellCharacters && (ShellCharacters)obj == ShellCharacters.Newline ? "\n" : obj;
        }

        public static ProgressBar OpenProgressBar (string identifier, string description)
        {
            return new ProgressBar (identifier: identifier, description: description);
        }

        public static void UserChoice (string msg, params UserChoice[] choices)
        {
            new UserChoices (choices).Ask (question: msg);
        }

        public static string AskForString (string question, LogColor color = LogColor.Reset)
        {
            Log.MessageLog ("Question => ", question);
            Console.ResetColor ();
            if (color != LogColor.Reset)
                Console.ForegroundColor = color.ToConsoleColor ();
            Console.Write (question.TrimEnd (' ') + " ");
            Console.ResetColor ();
            Console.Out.Flush ();
            string input = Console.ReadLine () ?? "";
            input = input.Trim (' ', '\r', '\n', '\t');
            Log.MessageLog ("User Input => ", input);
            return input;
        }

        private static Dictionary<string, TaggedLog> taggedLogs = new Dictionary<string, TaggedLog> ();

        public static void SetupTaggedLog (string logNamespace, int maxWidth)
        {
            taggedLogs [logNamespace] = new TaggedLog (maxWidth: maxWidth);
        }

        public static TaggedLog TaggedNamespace (string logNamespace)
        {
            if (taggedLogs.ContainsKey (logNamespace)) {
                return taggedLogs [logNamespace];
            } else {
                throw new ArgumentOutOfRangeException ("There is no such tagged log namespace: ", logNamespace);
            }
        }

        public static string FillOrCut (string text, uint length, string ending)
        {
            if (text.Length > length) {
                if (text.Length >= ending.Length) {
                    return text.Substring (0, (int)length - ending.Length) + ending;
                } else {
                    // WTF?
                    Log.Error ("The string in parameter 'ending' can be longer than the specified 'length'!");
                    Log.Error ("FillOrCut(text='", text, "', length=", length, ", ending='", ending, "')");
                    return text.Substring (0, (int)length);
                }
            } else {
                return text + String.Concat (Enumerable.Repeat (" ", (int)length - text.Length));
            }
        }

        public static string Fill (string text, uint length)
        {
            if (text.Length > length) {
                return text;
            } else {
                return text + String.Concat (Enumerable.Repeat (" ", (int)length - text.Length));
            }
        }
    }
}
