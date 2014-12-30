using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Shares;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Namespaces;
using Shell.Media.Content;

namespace Shell.Media
{
    public sealed class MediaShareManager : Library
    {
        public static bool DEBUG_SHARES = false;

        public string RootDirectory { get; private set; }

        public Dictionary<string, MediaShare> SharesByConfigFile { get; private set; }

        public MediaShare[] Shares { get { return SharesByConfigFile.Values.ToArray (); } }

        public MediaShareManager (string rootDirectory)
        {
            ConfigName = NamespacePictures.CONFIG_NAME;
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
                    MediaShare share = MediaShare.CreateInstance (file, filesystems: fs);

                    SharesByConfigFile [file] = share;
                } catch (IOException) {
                    Log.Error ("Can't open tree config file: ", file);
                } catch (ShareUnavailableException ex) {
                    if (DEBUG_SHARES) {
                        Log.Error (ex.Message);
                    } else {
                        Log.DebugLog (ex.Message);
                    }
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
                    s => s.Albums.Count
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

        public void Index (Filter shareFilter)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
                    Log.Message ("Share: ", share.Name);
                    Log.Indent++;
                    share.Index ();
                    Log.Indent--;
                }
            } else {
                Log.Message ("No shares are available for indexing.");
            }
        }

        public void Clean (Filter shareFilter)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter);

            if (filteredShares.Length != 0) {
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
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
            if (SharesByConfigFile.Count != 0) {
                foreach (MediaShare share in Shares.OrderBy (share => share.RootDirectory)) {
                    share.Serialize (verbose: true);
                }
            } else {
                Log.Message ("No shares are available for serializing.");
            }
        }

        public void Deserialize ()
        {
            if (SharesByConfigFile.Count != 0) {
                foreach (MediaShare share in Shares.OrderBy (share => share.RootDirectory)) {
                    share.Deserialize (verbose: false);
                }
            } else {
                Log.Message ("No shares are available for deserializing.");
            }
        }
    }
}
