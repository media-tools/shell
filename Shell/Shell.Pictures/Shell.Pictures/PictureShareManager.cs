using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;

namespace Shell.Pictures
{
    public sealed class PictureShareManager
    {
        public string RootDirectory { get; private set; }

        public Dictionary<string, PictureShare> PictureDirectories { get; private set; }

        private FileSystems fs;

        public PictureShareManager (string rootDirectory, FileSystems filesystems)
        {
            RootDirectory = rootDirectory;
            PictureDirectories = new Dictionary<string, PictureShare> ();
            fs = filesystems;
        }

        public void Initialize (bool cached)
        {
            if (!cached || !fs.Config.FileExists (path: "trees.txt")) {
                Func<FileInfo, bool> onlyPictureConfig = fileInfo => fileInfo.Name == PictureShare.PICTURE_CONFIG_FILENAME;
                IEnumerable<FileInfo> configFiles = Shell.FileSync.FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: onlyPictureConfig, dirFilter: dir => true);
                fs.Config.WriteAllLines (path: "trees.txt", contents: from file in configFiles
                                                                      where file.Name == PictureShare.PICTURE_CONFIG_FILENAME
                                                                      select file.FullName);
            }
            PictureDirectories.Clear ();
            string[] files = fs.Config.ReadAllLines (path: "trees.txt");
            foreach (string file in files) {
                try {
                    PictureShare share = PictureShare.CreateInstance (file, filesystems: fs);
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
                Log.Indent++;
                int i = 1;
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    Log.Message (share, ":");
                    Log.Indent++;
                    foreach (Album album in share.Albums) {
                        Log.Message ("- ", album);
                    }
                    Log.Indent--;
                    i++;
                }
                Log.Indent--;
                Log.Message ();
            } else {
                Log.Message ("No shares or directory trees available.");
            }
        }

        public void Index ()
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    share.Index ();
                }
            } else {
                Log.Message ("No shares are available for indexing.");
            }
        }

        public void Serialize ()
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    share.Serialize (verbose: true);
                }
            } else {
                Log.Message ("No shares are available for indexing.");
            }
        }

        public void Deserialize ()
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    share.Deserialize ();
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
