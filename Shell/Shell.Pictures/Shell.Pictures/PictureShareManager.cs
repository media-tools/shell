using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;

namespace Shell.Pictures
{
    public class PictureShareManager
    {
        public string RootDirectory { get; private set; }

        public Dictionary<string, PictureShare> PictureDirectories { get; private set; }

        public PictureShareManager (string rootDirectory)
        {
            RootDirectory = rootDirectory;
            PictureDirectories = new Dictionary<string, PictureShare> ();
        }

        public void Initialize (FileSystems filesystems, bool cached)
        {
            if (!cached || !filesystems.Config.FileExists (path: "trees.txt")) {
                Func<FileInfo, bool> onlyPictureConfig = fileInfo => fileInfo.Name == PictureShare.PICTURE_CONFIG_FILENAME;
                IEnumerable<FileInfo> configFiles = Shell.FileSync.FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: onlyPictureConfig, dirFilter: dir => true);
                filesystems.Config.WriteAllLines (path: "trees.txt", contents: from file in configFiles where file.Name == PictureShare.PICTURE_CONFIG_FILENAME select file.FullName);
            }
            PictureDirectories.Clear ();
            string[] files = filesystems.Config.ReadAllLines (path: "trees.txt");
            foreach (string file in files) {
                try {
                    PictureShare share = new PictureShare (file);
                    if (share.IsEnabled) {
                        PictureDirectories [file] = share;
                    } else {
                        Log.Error ("Can't use, not enabled: ", share);
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
            if (PictureDirectories.Count != 0) {
                Log.Message ("List of picture shares:");
                Log.Indent ++;
                int i = 1;
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    Log.Message (share, ":");
                    Log.Indent ++;
                    foreach (Album album in share.Albums) {
                        Log.Message ("- ", album);
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

        public void Index (FileSystems filesystems)
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    share.Index (filesystems);
                }
            } else {
                Log.Message ("No shares are available for indexing.");
            }
        }

        public void Sort ()
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    share.Sort ();
                }
            } else {
                Log.Message ("No shares are available for synchronization.");
            }
        }
    }
}

