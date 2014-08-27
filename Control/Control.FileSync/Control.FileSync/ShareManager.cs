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
            Func<FileInfo, bool> onlyTreeConfig = fileInfo => fileInfo.Name == Tree.TREE_CONFIG_FILENAME;
            IEnumerable<FileInfo> files = FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: onlyTreeConfig, dirFilter: dir => true);
            foreach (FileInfo file in files) {
                if (file.Name == Tree.TREE_CONFIG_FILENAME) {
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
                Log.Message ("Available shares (count: ", Shares.Count, "):");
                Log.Indent ++;
                int i = 1;
                foreach (Share share in from share in Shares.Values orderby share.Name select share) {
                    Log.Message (share, ":");
                    Log.Indent ++;
                    foreach (Tree tree in share.Trees) {
                        Log.Message ("- ", tree);
                    }
                    Log.Indent --;
                    i ++;
                }
                Log.Indent --;
            } else {
                Log.Message ("No shares or directory trees available.");
            }
        }

        public void Synchronize ()
        {
            if (Shares.Count != 0) {
                foreach (Share share in from share in Shares.Values orderby share.Name select share) {
                    share.Synchronize ();
                }
            } else {
                Log.Message ("No shares are available for synchronization.");
            }
        }
    }
}

