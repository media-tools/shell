using System;
using Shell.Common.Util;
using System.IO;
using Shell.Common.IO;
using System.Linq;

namespace Shell.Common.Shares
{
    public abstract class CommonShare<Derived> : ValueObject<Derived>
        where Derived : CommonShare<Derived>
    {
        public static string CONFIG_FILENAME = "control.ini";

        public string ConfigPath { get; private set; }

        public string RootDirectory { get; private set; }

        protected ConfigFile config;

        private string ConfigSection;

        public CommonShare (string path, string configSection)
        {
            ConfigSection = configSection;
            if (Path.GetFileName (path) == CONFIG_FILENAME) {
                RootDirectory = Path.GetDirectoryName (path);
                ConfigPath = path;
            } else {
                throw new ShareUnavailableException ("Illegal tree config file: " + path);
            }
            config = new ConfigFile (filename: ConfigPath);

            if (config.ContainsValue (ConfigSection, "root-paths-include")) {
                config [ConfigSection, "enabled-paths-include", ""] = config [ConfigSection, "root-paths-include", ""];
                config.RemoveValue (ConfigSection, "root-paths-include");
            }
            if (config.ContainsValue (ConfigSection, "root-paths-exclude")) {
                config [ConfigSection, "enabled-paths-exclude", ""] = config [ConfigSection, "root-paths-exclude", ""];
                config.RemoveValue (ConfigSection, "root-paths-exclude");
            }

            int fuuuck = (Name + IsEnabled + IsReadable + IsWriteable + IsDeletable + IsExperimental).GetHashCode ();
            fuuuck++;

            string shareType = this.GetType ().Name;

            if (EnabledPathsInclude.Length != 0 && !EnabledPathsInclude.Split (',', ';', '|').Any (p => path.ToLower ().Contains (p.ToLower ()))) {
                throw new ShareUnavailableException ("Path of " + shareType + " is not in the included root path list: path=" + path + ", include=" + EnabledPathsInclude);
            }
            if (EnabledPathsExclude.Length != 0 && EnabledPathsExclude.Split (',', ';', '|').Any (p => path.ToLower ().Contains (p.ToLower ()))) {
                throw new ShareUnavailableException ("Path of " + shareType + " is in the excluded root path list: path=" + path + ", exclude=" + EnabledPathsExclude);
            }

            IsReadableOverride = null;
            if (ReadPathsInclude.Length != 0) {
                IsReadableOverride = ReadPathsInclude.Split (',', ';', '|').Any (p => path.ToLower ().Contains (p.ToLower ()));
                Log.Debug ("Option 'read' of " + shareType + " is overridden: value=" + IsReadableOverride.Value + ", path=" + path + ", include=" + ReadPathsInclude);
            } else if (ReadPathsExclude.Length != 0) {
                IsReadableOverride = ReadPathsExclude.Split (',', ';', '|').Any (p => path.ToLower ().Contains (p.ToLower ()));
                Log.Debug ("Option 'read' of " + shareType + " is overridden: value=" + IsReadableOverride.Value + ", path=" + path + ", exclude=" + ReadPathsExclude);
            }

            IsWriteableOverride = null;
            if (WritePathsInclude.Length != 0) {
                IsWriteableOverride = WritePathsInclude.Split (',', ';', '|').Any (p => path.ToLower ().Contains (p.ToLower ()));
                Log.Debug ("Option 'write' of " + shareType + " is overridden: value=" + IsWriteableOverride.Value + ", path=" + path + ", include=" + WritePathsInclude);
            } else if (WritePathsExclude.Length != 0) {
                IsWriteableOverride = WritePathsExclude.Split (',', ';', '|').Any (p => path.ToLower ().Contains (p.ToLower ()));
                Log.Debug ("Option 'write' of " + shareType + " is overridden: value=" + IsWriteableOverride.Value + ", path=" + path + ", exclude=" + WritePathsExclude);
            }

            if (!IsEnabled) {
                throw new ShareUnavailableException ("Can't use " + shareType + ", not enabled: " + path);
            }

            if (Commons.IS_EXPERIMENTAL != IsExperimental) {
                throw new ShareUnavailableException ("Can't use " + shareType + " in " + (Commons.IS_EXPERIMENTAL ? "" : "not ") + "experimental mode: " + path);
            }
        }


        public string Name {
            get { return config [ConfigSection, "name", StringUtils.RandomShareName ()]; }
            set { config [ConfigSection, "name", StringUtils.RandomShareName ()] = value; }
        }

        public bool IsEnabled {
            get { return config [ConfigSection, "enabled", false]; }
            set { config [ConfigSection, "enabled", false] = value; }
        }

        public bool? IsReadableOverride { get; private set; }

        public bool IsReadable {
            get { return IsReadableOverride.HasValue ? IsReadableOverride.Value : config [ConfigSection, "read", true]; }
            set { config [ConfigSection, "read", true] = value; }
        }

        public bool? IsWriteableOverride { get; private set; }

        public bool IsWriteable {
            get { return IsWriteableOverride.HasValue ? IsWriteableOverride.Value : config [ConfigSection, "write", true]; }
            set { config [ConfigSection, "write", true] = value; }
        }

        public bool IsDeletable {
            get { return config [ConfigSection, "delete", false]; }
            set { config [ConfigSection, "delete", false] = value; }
        }

        public bool IsExperimental {
            get { return config [ConfigSection, "experimental", false]; }
            set { config [ConfigSection, "experimental", false] = value; }
        }

        public string EnabledPathsInclude {
            get { return config [ConfigSection, "enabled-paths-include", ""]; }
            set { config [ConfigSection, "enabled-paths-include", ""] = value; }
        }

        public string EnabledPathsExclude {
            get { return config [ConfigSection, "enabled-paths-exclude", ""]; }
            set { config [ConfigSection, "enabled-paths-exclude", ""] = value; }
        }

        public string ReadPathsInclude {
            get { return config [ConfigSection, "read-paths-include", ""]; }
            set { config [ConfigSection, "read-paths-include", ""] = value; }
        }

        public string ReadPathsExclude {
            get { return config [ConfigSection, "read-paths-exclude", ""]; }
            set { config [ConfigSection, "read-paths-exclude", ""] = value; }
        }

        public string WritePathsInclude {
            get { return config [ConfigSection, "write-paths-include", ""]; }
            set { config [ConfigSection, "write-paths-include", ""] = value; }
        }

        public string WritePathsExclude {
            get { return config [ConfigSection, "write-paths-exclude", ""]; }
            set { config [ConfigSection, "write-paths-exclude", ""] = value; }
        }
    }
}

