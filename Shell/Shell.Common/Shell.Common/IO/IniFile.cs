using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Shell.Common.Util;
using System.Threading;

namespace Shell.Common.IO
{
    public sealed class IniFile : IDisposable
    {
        private string Filename;
        public Dictionary<string, Dictionary<string, string>> Data;

        public IniFile (string filename)
        {
            Data = new Dictionary<string, Dictionary<string, string>> ();
            Filename = filename;
            if (File.Exists (filename)) {
                using (StreamReader reader = new StreamReader (filename)) {
                    string section = null;
                    while (reader.Peek () != -1) {
                        string line = StripComments (reader.ReadLine ().Trim ());
                        if (line.StartsWith ("[") && line.EndsWith ("]")) {
                            section = line.Substring (1, line.Length - 2);
                            if (!Data.ContainsKey (section)) {
                                Data [section] = new Dictionary<string,string> ();
                            }
                        } else if (line.Contains ("=")) {
                            string[] parts = line.Split ('=');
                            if (section != null) {
                                Data [section] [Decode (parts [0].Trim ())] = Decode (parts [1].Trim ());
                            }
                        }
                    }
                }
            }
        }

        [ExcludeFromCodeCoverageAttribute]
        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        [ExcludeFromCodeCoverageAttribute]
        private void Dispose (bool disposing)
        {
            if (disposing) {
                Save ();
            }
        }

        private static Object saveLock = new Object ();

        public void Save ()
        {
            if (!Commons.CanStartCriticalOperation) {
                Log.MessageLog ("Can't save because we are exiting.");
                return;
            }

            Commons.CriticalOperations++;
            lock (saveLock) {
                string tempFilename = Filename + ".tmp";
                using (StreamWriter writer = new StreamWriter (tempFilename)) {
                    foreach (string section in Data.Keys.OrderBy (x => x)) {
                        writer.WriteLine ("[" + section + "]");
                        foreach (string key  in Data [section].Keys.OrderBy (x => x)) {
                            writer.WriteLine (Encode (key) + "=" + Encode (Data [section] [key]));
                        }
                    }
                }
                File.Copy (sourceFileName: tempFilename, destFileName: Filename, overwrite: true);
                File.Delete (path: tempFilename);
            }
            Commons.CriticalOperations--;
        }

        private static string StripComments (string line)
        {
            if (line != null) {
                if (line.Contains ("//")) {
                    return line.Remove (line.IndexOf ("//")).Trim ();
                }
                return line.Trim ();
            }
            return string.Empty;
        }

        public string this [string section, string key, string defaultValue = null] {
            get {
                if (!Data.ContainsKey (section)) {
                    Data [section] = new Dictionary<string,string> ();
                }
                if (!Data [section].ContainsKey (key)) {
                    Data [section] [key] = defaultValue;
                    Save ();
                }
                string value = Data [section] [key];
                return value;
            }
            set {
                if (!Data.ContainsKey (section)) {
                    Data [section] = new Dictionary<string,string> ();
                }
                Data [section] [key] = value ?? "";
                Save ();
            }
        }

        private string Encode (string text)
        {
            return text.Replace ("\r", "").Replace ("\n", "\\n");
        }

        private string Decode (string text)
        {
            return text.Replace ("\\n", "\n");
        }

        public IEnumerable<string> Sections { get { return Data.Keys; } }

        public IEnumerable<string> KeysInSection (string section)
        {
            if (Data.ContainsKey (section)) {
                return Data [section].Keys;
            } else {
                return Enumerable.Empty<string> ();
            }
        }

        public Dictionary<string, string> SectionToDictionary (string section)
        {
            if (Data.ContainsKey (section)) {
                return Data [section].ToDictionary (entry => entry.Key, entry => entry.Value);
            } else {
                return new Dictionary<string, string> ();
            }
        }
    }
}
