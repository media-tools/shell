using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Control.Common
{
    [ExcludeFromCodeCoverageAttribute]
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

        private static void LogFileWrite (string text)
        {
            if (isNewLineInLogfile) {
                LogFilePrefix ();
            }
            try {
                logFile.Write (text);
                isNewLineInLogfile = false;
            } catch (Exception) {
            }
        }

        private static int k = 0;

        private static void LogFileWriteLine (string text)
        {
            if (isNewLineInLogfile) {
                LogFilePrefix ();
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
                logFile.Write (Commons.DATETIME_LOG + "  ");
            } catch (Exception) {
            }
        }

        // Lists
        private static Dictionary<string, ListDefinition> lists = new Dictionary<string, ListDefinition> ();
        private static string lastListId = null;

        public static void Debug (params object[] message)
        {
            EndList ();
            DebugConsole (message);
            DebugLog (message);
        }

        public static void DebugLog (params object[] message)
        {
            string str = string.Join ("", message);
            LogFileWriteLine (str);
        }

        public static void DebugConsole (params object[] message)
        {
            string str = string.Join ("", message);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine (str);
            Console.ResetColor ();
        }

        public static void Message (params object[] message)
        {
            EndList ();
            MessageConsole (message);
            MessageLog (message);
        }

        public static void MessageLog (params object[] message)
        {
            string str = string.Join ("", message);
            LogFileWriteLine (str);
        }

        public static void MessageConsole (params object[] message)
        {
            string str = string.Join ("", message);
            Console.WriteLine (str);
        }

        public static void Error (params object[] message)
        {
            EndList ();
            string str = string.Join ("", message);
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine (str);
            LogFileWriteLine (str);
            logFile.Flush ();
            Console.ResetColor ();
        }

        private static void List (object id, object before, object after, object begin, object end)
        {
            ListDefinition def = new ListDefinition {
                Id = id.ToString (),
                Before = before.ToString (),
                After = after.ToString (),
                Begin = begin.ToString (),
                End = end.ToString ()
            };
            lists [def.Id] = def;
        }

        public static void BlockList (object id, object before, object after, object begin, object end)
        {
            string beforeStr = before.ToString ();
            string afterStr = after + Environment.NewLine;
            string beginStr = begin + Environment.NewLine;
            string endStr = end.ToString ().Length > 0 ? end + Environment.NewLine : "";
            List (id, beforeStr, afterStr, beginStr, endStr);
        }

        public static void InlineList (object id, object before, object after, object begin, object end)
        {
            List (id, before, after, begin, end + Environment.NewLine);
        }

        public static void EndList ()
        {
            if (lastListId != null) {
                Console.Write (lists [lastListId].End);
                LogFileWrite (lists [lastListId].End);
                lastListId = null;
            }
        }

        public static void ListElement (object id, string element)
        {
            if (lists.ContainsKey (id.ToString ())) {
                ListDefinition def = lists [id.ToString ()];
                if (lastListId != id.ToString ()) {
                    EndList ();
                    Console.Write (def.Begin);
                    LogFileWrite (def.Begin);
                }
                Console.Write (def.Before);
                Console.Write (element.ToString ());
                Console.Write (def.After);
                LogFileWrite (def.Before);
                LogFileWrite (element.ToString ());
                LogFileWrite (def.After);

                lastListId = id.ToString ();
            } else {
                Message ("Error! Invalid list ID in ListElement (", id, ", ", element, ")");
            }
        }

        public static void ListElement (object id, params object[] element)
        {
            ListElement (id, string.Join ("", element));
        }

        private struct ListDefinition
        {
            public string Id;
            public string Before;
            public string After;
            public string Begin;
            public string End;
        }
    }
}
