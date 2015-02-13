using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Shares;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Media.Content;
using Shell.Media.Pictures;
using Shell.Namespaces;

namespace Shell.Media
{
    public sealed class MediaShareManager : Library
    {
        public static bool DEBUG_DISABLED_SHARES = false;

        public string RootDirectory { get; private set; }

        public Dictionary<string, MediaShare> SharesByConfigFile { get; private set; }

        public MediaShare[] Shares { get { return SharesByConfigFile.Values.ToArray (); } }

        public MediaShareManager (string rootDirectory)
        {
            ConfigName = NamespaceMedia.CONFIG_NAME;
            RootDirectory = rootDirectory;
            SharesByConfigFile = new Dictionary<string, MediaShare> ();
        }

        public void Initialize (bool cached)
        {
            if (!cached || !fs.Config.FileExists (path: "trees.txt")) {
                Func<FileInfo, bool> onlyPictureConfig = fileInfo => fileInfo.Name == CommonShare<MediaShare>.CONFIG_FILENAME;
                IEnumerable<FileInfo> configFiles = FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: onlyPictureConfig, dirFilter: dir => true, followSymlinks: false);
                fs.Config.WriteAllLines (path: "trees.txt", contents: from file in configFiles
                                                                                  where file.Name == CommonShare<MediaShare>.CONFIG_FILENAME
                                                                                  select file.FullName);
            }
            SharesByConfigFile.Clear ();
            string[] files = fs.Config.ReadAllLines (path: "trees.txt");
            foreach (string file in files) {
                try {
                    MediaShare share = MediaShare.CreateInstance (configPath: file, filesystems: fs);

                    SharesByConfigFile [file] = share;

                } catch (IOException ex) {
                    Log.Error ("Can't open tree config file: ", file);
                    Log.Indent++;
                    Log.Error ("Message: ", ex.Message);
                    Log.Error (ex);
                    Log.Indent--;

                } catch (ShareUnavailableException ex) {
                    Log.Indent++;
                    if (DEBUG_DISABLED_SHARES) {
                        Log.Error (ex.Message);
                    } else {
                        Log.DebugLog (ex.Message);
                    }
                    Log.Indent--;
                }
            }
        }

        public void PrintShares (Filter shareFilter)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                Log.Message ("List of picture shares:");
                Log.Indent++;
                Log.Message (filteredShares.OrderBy (s => s.RootDirectory).ToStringTable (
                    s => LogColor.Reset,
                    new[] { "Name", "Root Directory", "Album Count" },
                    s => s.Name,
                    s => s.RootDirectory,
                    s => s.Database.AlbumCount
                ));
                Log.Indent--;
            } else {
                Log.Message ("No shares available.");
            }
        }

        public void PrintAlbums (Filter shareFilter, Filter albumFilter)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                Log.Message ("List of picture shares:");
                Log.Indent++;
                int i = 1;
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
                    share.PrintAlbums (albumFilter: albumFilter);
                    i++;
                }
                Log.Indent--;
            } else {
                Log.Message ("No shares available.");
            }
        }

        public void UpdateIndex (Filter shareFilter)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
                    Log.Message ("Share: ", share.Name);
                    Log.Indent++;
                    share.UpdateIndex ();
                    Log.Indent--;
                }
            } else {
                Log.Message ("No shares are available for indexing.");
            }
        }

        public void CleanIndex (Filter shareFilter)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
                    Log.Message ("Share: ", share.Name);
                    Log.Indent++;
                    share.CleanIndex ();
                    Log.Indent--;
                }
            } else {
                Log.Message ("No shares are available for cleaning.");
            }
        }

        public void RebuildIndex (Filter shareFilter, Filter albumFilter)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
                    Log.Message ("Share: ", share.Name);
                    Log.Indent++;
                    share.RebuildIndex (albumFilter: albumFilter);
                    Log.Indent--;
                }
            } else {
                Log.Message ("No shares are available for indexing.");
            }
        }

        public void DeduplicateAlbums (Filter shareFilter, Filter albumFilter, bool dryRun)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
                    Log.Message ("Share: ", share.Name);
                    Log.Indent++;
                    PictureDeduplication dedup = new PictureDeduplication ();
                    dedup.DeduplicateAlbums (share: share, albumFilter: albumFilter, dryRun: dryRun);
                    Log.Indent--;
                }
            } else {
                Log.Message ("No shares are available for deduplicating.");
            }
        }

        public void DeduplicateShares (Filter shareFilter, Filter albumFilter, bool dryRun)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
                    Log.Message ("Share: ", share.Name);
                    Log.Indent++;
                    PictureDeduplication dedup = new PictureDeduplication ();
                    dedup.DeduplicateShare (share: share, albumFilter: albumFilter, dryRun: dryRun);
                    Log.Indent--;
                }
            } else {
                Log.Message ("No shares are available for deduplicating.");
            }
        }

        public void SetAuthor (Filter shareFilter, Filter albumFilter, string author, bool dryRun)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
                    Log.Message ("Share: ", share.Name);
                    Log.Indent++;
                    PictureDeduplication dedup = new PictureDeduplication ();
                    dedup.SetAuthor (share: share, albumFilter: albumFilter, author: author, dryRun: dryRun);
                    Log.Indent--;
                }
            } else {
                Log.Message ("No shares are available for author setting.");
            }
        }

        /* public void Serialize ()
        {
            if (SharesByConfigFile.Count != 0) {
                foreach (MediaShare share in Shares.OrderBy (share => share.RootDirectory)) {
                    share.Serialize (verbose: true);
                }
            } else {
                Log.Message ("No shares are available for serializing.");
            }
        } */

        /*
        public void Deserialize ()
        {
            if (SharesByConfigFile.Count != 0) {
                foreach (MediaShare share in Shares.OrderBy (share => share.RootDirectory)) {
                    share.Deserialize (verbose: true);
                }
            } else {
                Log.Message ("No shares are available for deserializing.");
            }
        }*/

        public void GarbageCollection ()
        {
            if (SharesByConfigFile.Count != 0) {
                foreach (MediaShare share in Shares.OrderBy (share => share.RootDirectory)) {
                    share.GarbageCollection ();
                }
            } else {
                Log.Message ("No shares are available for deserializing.");
            }
        }
    }
}
