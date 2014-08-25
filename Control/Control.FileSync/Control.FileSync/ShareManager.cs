using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Control.Common;
using Control.Common.IO;
using Control.Common.Tasks;

namespace Control.FileSync
{
    class ShareManager
    {
        public string RootDirectory { get; private set; }

        public Dictionary<string, Share> Shares { get; private set; }

        public ShareManager (string rootDirectory)
        {
            RootDirectory = rootDirectory;
            Shares = new Dictionary<string, Share> ();
        }

        public void Initialize ()
        {
            Shares.Clear ();
            IEnumerable<FileInfo> files = FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: file => true, dirFilter: dir => true);
            foreach (FileInfo file in files) {
                if (file.Name == "control.ini") {
                    Log.MessageConsole ("  ", file.FullName);
                    Tree tree = new Tree (file.FullName);
                    if (!Shares.ContainsKey (tree.Name)) {
                        Shares [tree.Name] = new Share (tree.Name);
                    }
                    Shares [tree.Name].Add (tree);
                }
            }
        }

        public void Print ()
        {
            if (Shares.Count != 0) {
                Log.Message ("The following shares and directory trees are available:");
                foreach (Share share in from share in Shares.Values orderby share.Name select share) {
                    Log.Message ("  - ", share);
                    foreach (Tree tree in share.Trees) {
                        Log.Message ("    - ", tree);
                    }
                }
            } else {
                Log.Message ("No shares or directory trees available.");
            }
        }
    }
}

