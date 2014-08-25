using System;
using System.IO;
using Control.Common.IO;

namespace Control.FileSync
{
    public class Tree
    {
        public static string TREE_CONFIG_FILENAME = "control.ini";
        private static string CONFIG_SECTION = "FileSync";

        private ConfigFile config;
        public string ConfigPath { get; private set; }
        public string RootDirectory { get; private set; }

        public Tree (string path)
        {
            if (Path.GetFileName (path) == TREE_CONFIG_FILENAME) {
                RootDirectory = Path.GetDirectoryName (path);
                ConfigPath = path;
            } else {
                throw new ArgumentException ("Illegal tree config file: " + path);
        }http://de.wiktionary.org/wiki/Apfelbutzen

            config = new ConfigFile (filename: ConfigPath);
        }

        public bool IsEnabled {
            get { return config [CONFIG_SECTION, "enabled", true]; }
            set { config [CONFIG_SECTION, "enabled", true] = value; }
        }

        public bool IsReadable {
            get { return config [CONFIG_SECTION, "read", true]; }
            set { config [CONFIG_SECTION, "read", true] = value; }
        }

        public bool IsWriteable {
            get { return config [CONFIG_SECTION, "write", true]; }
            set { config [CONFIG_SECTION, "write", true] = value; }
        }

        public bool IsDeletable {
            get { return config [CONFIG_SECTION, "delete", false]; }
            set { config [CONFIG_SECTION, "delete", false] = value; }
        }


    }
}

