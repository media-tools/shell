using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Control.Common
{
    [ExcludeFromCodeCoverageAttribute]
    public static partial class SystemInfo
    {
        
        public static bool IsRunningOnMono ()
        {
            return Type.GetType ("Mono.Runtime") != null;
        }

        public static bool IsRunningOnMonogame ()
        {
            return true;
        }

        public static bool IsRunningOnLinux ()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix;
        }

        public static bool IsRunningOnWindows ()
        {
            return !IsRunningOnLinux ();
        }
        

        public static string SettingsDirectory
        {
            get {
                if (settingsDirectory != null) {
                    return settingsDirectory;
                }
                else {
                    string directory;
                    if (SystemInfo.IsRunningOnLinux ()) {
                        directory = Environment.GetEnvironmentVariable ("HOME") + "/.config/control/";
                    }
                    else {
                        directory = Environment.GetFolderPath (System.Environment.SpecialFolder.UserProfile) + "\\Control\\";
                    }
                    Directory.CreateDirectory (directory);
                    return settingsDirectory = directory;
                }
            }
            set {
                settingsDirectory = value;
            }
        }

        private static string settingsDirectory = null;
        

        public static string CacheDirectory
        {
            get {
                if (cacheDirectory != null) {
                    return cacheDirectory;
                }
                else {
                    string directory;
                    if (SystemInfo.IsRunningOnLinux ()) {
                        directory = Environment.GetEnvironmentVariable ("HOME") + "/.cache/control/";
                    }
                    else {
                        directory = Environment.GetFolderPath (System.Environment.SpecialFolder.UserProfile) + "\\ControlCache\\";
                    }
                    Directory.CreateDirectory (directory);
                    return cacheDirectory = directory;
                }
            }
            set {
                cacheDirectory = value;
            }
        }

        private static string cacheDirectory = null;

        
        public static string LogDirectory
        {
            get {
                string directory = SettingsDirectory + "logs";
                Directory.CreateDirectory (directory);
                return directory;
            }
        }
        public static string ConfigDirectory
        {
            get {
                string directory = SettingsDirectory + "config";
                Directory.CreateDirectory (directory);
                return directory;
            }
        }
        public static string RuntimeDirectory
        {
            get {
                string directory = CacheDirectory + "runtime";
                Directory.CreateDirectory (directory);
                return directory;
            }
        }

        public readonly static char PathSeparator = Path.DirectorySeparatorChar;
    }
}