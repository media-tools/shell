using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Shares;

namespace Shell.FileSync
{
    public class ShareManager
    {
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
                Func<FileInfo, bool> onlyTreeConfig = fileInfo => fileInfo.Name == CommonShare<Tree>.CONFIG_FILENAME;
                IEnumerable<FileInfo> configFiles = FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: onlyTreeConfig, dirFilter: dir => true, followSymlinks: false);
                filesystems.Config.WriteAllLines (path: "trees.txt", contents: from file in configFiles
                                                                                           where file.Name == CommonShare<Tree>.CONFIG_FILENAME
                                                                                           select file.FullName);
            }

            Log.Info ("Load share config files:");
            Log.Indent++;
            Shares.Clear ();
            List<Tuple<string, string>> configTable = new List<Tuple<string, string>> ();
            string[] files = filesystems.Config.ReadAllLines (path: "trees.txt");
            foreach (string file in files) {
                try {
                    Tree tree = new Tree (file);
                    if (!Shares.ContainsKey (tree.Name)) {
                        Shares [tree.Name] = new Share (tree.Name);
                    }
                    Shares [tree.Name].Add (tree);
                    configTable.Add (Tuple.Create (file, "OK"));
                } catch (IOException) {
                    // Log.Error ("Can't open tree config file: ", file);
                    configTable.Add (Tuple.Create (file, "Error: Can't open tree config file"));
                } catch (ShareUnavailableException ex) {
                    // Log.Error (ex.Message);
                    configTable.Add (Tuple.Create (file, "Error: " + ex.Message.Replace (file, "[...]")));
                }
            }
            Log.Indent--;
            Log.Info (configTable.ToStringTable (
                i => LogColor.Reset,
                new[] { "Config File", "Status" },
                i => i.Item1,
                i => i.Item2
            ));
        }

        public void Print ()
        {
            if (Shares.Count != 0) {
                Log.Info ("List of shares:");
                Log.Indent++;
                int i = 1;
                foreach (Share share in from share in Shares.Values orderby share.Name select share) {
                    Log.Info (share, ":");
                    Log.Indent++;
                    Log.Info (share.Trees.ToStringTable (
                        s => LogColor.Reset,
                        new[] { "Name", "Root Directory", "Enabled", "Read", "Write", "Delete" },
                        s => s.Name,
                        s => s.RootDirectory,
                        s => s.IsEnabled,
                        s => s.IsReadable,
                        s => s.IsWriteableOverride.HasValue ? (s.IsWriteable ? "True  (o)" : "False (o)") : s.IsWriteable + "",
                        s => s.IsDeletable
                    ));
                    Log.Indent--;
                    i++;
                }
                Log.Indent--;
            } else {
                Log.Info ("No shares or directory trees available.");
            }
        }

        public void Synchronize ()
        {
            if (Shares.Count != 0) {
                foreach (Share share in from share in Shares.Values orderby share.Name select share) {
                    Log.Info (share, ":");
                    Log.Indent++;
                    share.Synchronize ();
                    Log.Indent--;
                    Log.Info ();
                }
            } else {
                Log.Info ("No shares are available for synchronization.");
            }
        }
    }
}

