using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;

namespace Shell.FileSync
{
    public class ShareManager
    {
        public static string TREE_LIST_CONFIG = "control.ini";

        public string RootDirectory { get; private set; }

        public Dictionary<string, Share> Shares { get; private set; }

        public ShareManager (string rootDirectory)
        {
            RootDirectory = rootDirectory;
            Shares = new Dictionary<string, Share> ();
        }

        public void Initialize (FileSystems filesystems, bool cached)
        {
            if (!cached || !filesystems.Config.FileExists (path: "trees.txt")) {
                Func<FileInfo, bool> onlyTreeConfig = fileInfo => fileInfo.Name == Tree.TREE_CONFIG_FILENAME;
                IEnumerable<FileInfo> configFiles = FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: onlyTreeConfig, dirFilter: dir => true);
                filesystems.Config.WriteAllLines (path: "trees.txt", contents: from file in configFiles where file.Name == Tree.TREE_CONFIG_FILENAME select file.FullName);
            }
            Shares.Clear ();
            string[] files = filesystems.Config.ReadAllLines (path: "trees.txt");
            foreach (string file in files) {
                try {
                    Tree tree = new Tree (file);
                    if (tree.IsEnabled) {
                        if (!Shares.ContainsKey (tree.Name)) {
                            Shares [tree.Name] = new Share (tree.Name);
                        }
                        Shares [tree.Name].Add (tree);
                    } else {
                        Log.Error ("Can't use, not enabled: ", tree);
                    }
                } catch (IOException) {
                    Log.Error ("Can't open tree config file: ", file);
                } catch (ArgumentException ex) {
                    Log.Error (ex);
                }
            }
        }

        public void Print ()
        {
            if (Shares.Count != 0) {
                Log.Message ("List of shares:");
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
                Log.Message ();
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

