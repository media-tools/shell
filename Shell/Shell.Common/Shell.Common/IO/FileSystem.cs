using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace Shell.Common.IO
{
    public class FileSystem
    {
        public string RootDirectory = "";
        private static HashSet<string> InstalledPackages = new HashSet<string> ();

        public FileSystem (ScriptTask task, FileSystemType type)
            : this (type: type)
        {
            RootDirectory += SystemInfo.PathSeparator + task.ConfigName;
            Directory.CreateDirectory (RootDirectory);

        }

        public FileSystem (Library library, FileSystemType type)
            : this (type: type)
        {
            RootDirectory += SystemInfo.PathSeparator + library.ConfigName;
            Directory.CreateDirectory (RootDirectory);

        }

        public FileSystem (FileSystemType type)
        {
            RootDirectory = (type == FileSystemType.Config ? SystemInfo.SettingsDirectory + SystemInfo.PathSeparator + "config" : SystemInfo.CacheDirectory + SystemInfo.PathSeparator + "runtime");
            Directory.CreateDirectory (RootDirectory);
        }

        public ConfigFile Config {
            get {
                if (_default == null) {
                    _default = new ConfigFile (RootDirectory + "main.ini");
                }
                return _default;
            }
            set {
                _default = value;
            }
        }

        private static ConfigFile _default;

        public bool FileExists (string path)
        {
            return File.Exists (RootDirectory + SystemInfo.PathSeparator + path);
        }

        public bool DirectoryExists (string path)
        {
            return Directory.Exists (RootDirectory + SystemInfo.PathSeparator + path);
        }

        public void WriteAllLines (string path, string[] contents)
        {
            File.WriteAllLines (RootDirectory + SystemInfo.PathSeparator + path, contents);
        }

        public void WriteAllLines (string path, IEnumerable<string> contents)
        {
            File.WriteAllLines (RootDirectory + SystemInfo.PathSeparator + path, contents);
        }

        public void WriteAllText (string path, string contents)
        {
            File.WriteAllText (RootDirectory + SystemInfo.PathSeparator + path, contents);
        }

        public string[] ReadAllLines (string path)
        {
            return File.ReadAllLines (RootDirectory + SystemInfo.PathSeparator + path);
        }

        public string ReadAllText (string path)
        {
            return File.ReadAllText (RootDirectory + SystemInfo.PathSeparator + path);
        }

        public ConfigFile OpenConfigFile (string name)
        {
            return new ConfigFile (filename: RootDirectory + SystemInfo.PathSeparator + name);
        }

        public FileStream Open (string name, FileMode mode, FileAccess access)
        {
            return File.Open (path: RootDirectory + SystemInfo.PathSeparator + name, mode: mode, access: access);
        }

        public HexString HashOfFile (string name)
        {
            return FileSystemUtilities.HashOfFile (path: RootDirectory + SystemInfo.PathSeparator + name);
        }

        private static JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        public void Serialize <A> (string path, IEnumerable<A> enumerable)
        {
            File.WriteAllText (RootDirectory + SystemInfo.PathSeparator + path, JsonConvert.SerializeObject (enumerable, Formatting.Indented, jsonSerializerSettings));
        }

        public void Deserialize <A> (string path, out List<A> list)
        {
            list = JsonConvert.DeserializeObject<List<A>> (File.ReadAllText (RootDirectory + SystemInfo.PathSeparator + path), jsonSerializerSettings);
        }

        public void ExecuteScript (string path, Action<string> receiveOutput = null, bool verbose = true, bool debug = true, bool sudo = false, bool ignoreEmptyLines = false)
        {
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo () {
                CreateNoWindow = false,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            startInfo.EnvironmentVariables ["LC_ALL"] = "C";
            if (sudo) {
                startInfo.FileName = "/usr/bin/sudo";
                startInfo.Arguments = "/bin/bash -x \"" + RootDirectory + SystemInfo.PathSeparator + path + "\"";
            } else {
                startInfo.FileName = "/bin/bash";
                startInfo.Arguments = "-x \"" + RootDirectory + SystemInfo.PathSeparator + path + "\"";
            }

            try {
                using (Process process = new Process ()) {
                    process.EnableRaisingEvents = true;
                    process.StartInfo = startInfo;
                    Action<object, DataReceivedEventArgs> actionWrite = (sender, e) => {
                        if (ignoreEmptyLines && string.IsNullOrWhiteSpace (e.Data)) {
                            // the line is empty, ignore it
                        } else {
                            if (verbose && (debug || !e.Data.StartsWith ("+ "))) {
                                Log.DebugConsole ("    ", e.Data);
                            }
                            Log.DebugLog ("    ", e.Data);
                            if (receiveOutput != null && !e.Data.StartsWith ("+ ")) {
                                receiveOutput (e.Data);
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) => actionWrite (sender, e);
                    process.OutputDataReceived += (sender, e) => actionWrite (sender, e);
                    
                    Log.MessageLog ("Start Process (date='", Commons.DATETIME, "', run='", RootDirectory + SystemInfo.PathSeparator + path, "')");
                    process.Start ();
                    process.BeginOutputReadLine ();
                    process.BeginErrorReadLine ();
                    process.WaitForExit ();
                    Log.MessageLog ("Stop Process (date='", Commons.DATETIME, "')");
                }
            } catch (Exception e) {
                Log.Error (e);
            }
        }

        public void RequirePackages (params string[] packages)
        {
            packages = (from pkg in packages
                                 where !InstalledPackages.Contains (pkg)
                                 select pkg).ToArray ();

            if (packages.Length > 0) {
                string script = "export TOINSTALL=\"\";\n";
                foreach (string pkg in packages) {
                    script += "if [ $(dpkg-query -W -f='${Status}' " + pkg + " 2>/dev/null | grep -c \"ok installed\") -eq 0 ];\nthen\n  export TOINSTALL=\"$TOINSTALL " + pkg + "\";\nfi\n";
                }
                script += "echo $TOINSTALL | egrep \"...\" >/dev/null && sudo apt-get install -fyqm $TOINSTALL\n";
                WriteAllText ("apt.sh", script);
                ExecuteScript (path: "apt.sh", verbose: false);
            }

            foreach (string pkg in packages) {
                InstalledPackages.Add (pkg);
            }
        }
    }
}

