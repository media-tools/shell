using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Shares;
using Shell.Common.Tasks;
using Shell.Pictures.Content;

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
                Func<FileInfo, bool> onlyPictureConfig = fileInfo => fileInfo.Name == CommonShare<PictureShare>.CONFIG_FILENAME;
                IEnumerable<FileInfo> configFiles = FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: onlyPictureConfig, dirFilter: dir => true, symlinks: false);
                fs.Config.WriteAllLines (path: "trees.txt", contents: from file in configFiles
                                                                                  where file.Name == CommonShare<PictureShare>.CONFIG_FILENAME
                                                                                  select file.FullName);
            }
            PictureDirectories.Clear ();
            string[] files = fs.Config.ReadAllLines (path: "trees.txt");
            foreach (string file in files) {
                try {
                    PictureShare share = PictureShare.CreateInstance (file, filesystems: fs);

                    PictureDirectories [file] = share;
                } catch (IOException) {
                    Log.Error ("Can't open tree config file: ", file);
                } catch (ShareUnavailableException ex) {
                    Log.Error (ex.Message);
                }
            }
        }

        private string printCount (int count, string ifZero)
        {
            return count > 0 ? count + "" : ifZero;
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
                    Log.Message (share.Albums.ToStringTable (
                        a => LogColor.Reset,
                        new[] { "Album", "Picture Files", "Audio Files", "Video Files", "Document Files" },
                        a => a.AlbumPath,
                        a => printCount (count: a.Files.Count (f => f.Medium is Picture), ifZero: ""),
                        a => printCount (count: a.Files.Count (f => f.Medium is Audio), ifZero: ""),
                        a => printCount (count: a.Files.Count (f => f.Medium is Video), ifZero: ""),
                        a => printCount (count: a.Files.Count (f => f.Medium is Document), ifZero: "")
                    ));
                    Log.Indent--;
                    i++;
                }
                Log.Indent--;
            } else {
                Log.Message ("No shares or directory trees available.");
            }
        }

        public void Index ()
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    Log.Message ("Share: ", share.Name);
                    Log.Indent++;
                    share.Index ();
                    Log.Indent--;
                }
            } else {
                Log.Message ("No shares are available for indexing.");
            }
        }

        public void Clean ()
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    Log.Message ("Share: ", share.Name);
                    Log.Indent++;
                    share.Clean ();
                    Log.Indent--;
                }
            } else {
                Log.Message ("No shares are available for cleaning.");
            }
        }

        public void Serialize ()
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    share.Serialize (verbose: true);
                }
            } else {
                Log.Message ("No shares are available for serializing.");
            }
        }

        public void Deserialize ()
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    share.Deserialize ();
                }
            } else {
                Log.Message ("No shares are available for deserializing.");
            }
        }

        public void Sort ()
        {
            if (PictureDirectories.Count != 0) {
                foreach (PictureShare share in from share in PictureDirectories.Values orderby share.RootDirectory select share) {
                    share.Sort ();
                }
            } else {
                Log.Message ("No shares are available for sorting.");
            }
        }
    }
}
