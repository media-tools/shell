using System;
using System.IO;
using System.Diagnostics;

namespace Control.Common
{
    public class FileSystem
    {
        public string RootDirectory = "";

        public FileSystem (Task task, FileSystemType type)
            : this (type: type)
        {
            RootDirectory += SystemInfo.PathSeparator + task.Name;
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

        public void ExecuteScript (string path)
        {
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo () {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = "/bin/bash",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = "-x \"" + RootDirectory + SystemInfo.PathSeparator + path + "\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            try {
                using (Process process = new Process()) {
                    process.EnableRaisingEvents = true;
                    process.StartInfo = startInfo;
                    Action<object, DataReceivedEventArgs> actionWrite = (sender, e) =>
                    {
                        Log.Debug ("    ", e.Data);
                    };

                    process.ErrorDataReceived += (sender, e) => actionWrite (sender, e);
                    process.OutputDataReceived += (sender, e) => actionWrite (sender, e);
                    
                    Log.Message ("Start Process (date='", Commons.DATETIME, "', run='", RootDirectory + SystemInfo.PathSeparator + path, "')");
                    process.Start ();
                    process.BeginOutputReadLine ();
                    process.BeginErrorReadLine ();
                    process.WaitForExit ();
                    Log.Message ("Stop Process (date='", Commons.DATETIME, "')");
                }
            } catch (Exception e) {
                Log.Error (e);
            }
        }

        public void RequirePackages (params string[] packages)
        {
            string script = "export TOINSTALL=\"\";\n";
            foreach (string pkg in packages) {
                script += "if [ $(dpkg-query -W -f='${Status}' " + pkg + " 2>/dev/null | grep -c \"ok installed\") -eq 0 ];\nthen\n  export TOINSTALL=\"$TOINSTALL " + pkg + "\";\nfi\n";
            }
            script += "echo $TOINSTALL | egrep \"...\" >/dev/null && sudo apt-get install -fyqm $TOINSTALL\n";
            WriteAllText ("apt.sh", script);
            ExecuteScript ("apt.sh");
        }
    }
}

