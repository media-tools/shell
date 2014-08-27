using System;
using System.IO;
using Control.Common.IO;
using System.Linq;
using System.Collections.Generic;

namespace Control.FileSync
{
    public class Tree
    {
        public static string TREE_CONFIG_FILENAME = "control.ini";
        private static string CONFIG_SECTION = "FileSync";

        public string ConfigPath { get; private set; }

        public string RootDirectory { get; private set; }

        public IEnumerable<DataFile> Files { get { return from file in FileMap.Keys select file; } }
        
        public bool IsSource { get; set; }
        public bool IsDestination { get; set; }

        private Dictionary<DataFile, DataFile> FileMap;
        private ConfigFile config;

        public Tree (string path)
        {
            if (Path.GetFileName (path) == TREE_CONFIG_FILENAME) {
                RootDirectory = Path.GetDirectoryName (path);
                ConfigPath = path;
            } else {
                throw new ArgumentException ("Illegal tree config file: " + path);
            }

            config = new ConfigFile (filename: ConfigPath);
            int fuuuck = (Name + IsEnabled + IsReadable + IsWriteable).GetHashCode ();
            fuuuck++;
        }

        public string Name {
            get { return config [CONFIG_SECTION, "name", RandomString ()]; }
            set { config [CONFIG_SECTION, "name", RandomString ()] = value; }
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

        public void CreateIndex ()
        {
            if (FileMap == null) {
                FileMap = new Dictionary<DataFile, DataFile> ();
                Func<FileInfo, bool> excludeTreeConfig = fileInfo => fileInfo.Name != Tree.TREE_CONFIG_FILENAME;
                IEnumerable<FileInfo> files = FileSystemLibrary.GetFileList (rootDirectory: RootDirectory, fileFilter: excludeTreeConfig, dirFilter: dir => true);
                foreach (FileInfo fileInfo in files) {
                    DataFile file = new DataFile (fileInfo: fileInfo, tree: this);
                    FileMap [file] = file;
                }
            }
            
            Log.DebugLog ("Index of: ", RootDirectory);
            Log.Indent ++;
            foreach (DataFile file in Files) {

                Log.DebugLog ("- ", file);
            }
            Log.Indent --;
        }

        public bool ContainsFile (DataFile search, out DataFile result)
        {
            if (FileMap.ContainsKey (search)) {
                result = FileMap [search];
                return true;
            } else {
                result = null;
                return false;
            }
        }

        public override string ToString ()
        {
            return string.Format ("Tree(name=\"{0}\", rootDirectory=\"{1}\", enabled={2}, read={3}, write={4}, delete={5})", Name, RootDirectory, IsEnabled, IsReadable, IsWriteable, IsDeletable);
        }

        public override int GetHashCode ()
        {
            return ToString ().GetHashCode ();
        }

        public override bool Equals (object obj)
        {
            return Equals (obj as Tree);
        }

        public bool Equals (Tree obj)
        {
            return obj != null && GetHashCode () == obj.GetHashCode ();
        }

        private static string RandomString ()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToLower ();
            var random = new Random ();
            var result = new string (Enumerable.Repeat (chars, 8).Select (s => s [random.Next (s.Length)]).ToArray ());
            return result;
        }
    }
}

