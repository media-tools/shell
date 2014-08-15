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
        private static StreamWriter logFile;

        public static void Init (string program = "game", string version = "")
        {
            try {
                string filename = (string.IsNullOrWhiteSpace (version) ? program : program + "-" + version) + ".log";
                logFile = File.AppendText (SystemInfo.LogDirectory + SystemInfo.PathSeparator + filename);
            } catch (Exception ex) {
                // we don't give a fuck whether we can open a log file
                logFile = StreamWriter.Null;
                Console.WriteLine (ex.ToString ());
            }
        }

        private static void LogFileWrite (string text)
        {
            try {
                logFile.Write (text);
            } catch (Exception) {
            }
        }

        private static int k = 0;

        private static void LogFileWriteLine (string text)
        {
            try {
                logFile.WriteLine (text);
                if (k % 100 == 0) {
                    logFile.Flush ();
                }
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
            Console.WriteLine (string.Join ("", message));
            LogFileWriteLine (string.Join ("", message));
        }

        public static void Error (Exception ex)
        {
            EndList ();
            Console.WriteLine (ex.ToString ());
            LogFileWriteLine (ex.ToString ());
            logFile.Flush ();
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
