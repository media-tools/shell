using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Shares;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Compatibility;
using Shell.Media.Content;
using Shell.Media.Files;
using System.Text.RegularExpressions;

namespace Shell.Media
{
    public sealed class MediaShare : CommonShare<MediaShare>
    {
        private static Dictionary<string, MediaShare> Instances = new Dictionary<string, MediaShare> ();

        private static string CONFIG_SECTION = "Pictures";

        public Dictionary<string, Album> AlbumMap { get; private set; }

        public Album[] Albums { get { return AlbumMap.Values.ToArray (); } }

        public Dictionary<HexString, Medium> Media { get; private set; }

        private ConfigFile serializedAlbums;
        private ConfigFile serializedMedia;

        private FileSystems fs;

        private HashSet<string> _highQualityAlbums;

        public HashSet<string> HighQualityAlbums {
            get {
                return _highQualityAlbums = _highQualityAlbums ?? new HashSet<string> (config [CONFIG_SECTION, "high-quality-albums", ""].Split (new char[] {
                    ':',
                    ';'
                }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public string SpecialAlbumPrefix {
            get { return config [CONFIG_SECTION, "album-prefix-special", ""]; }
            set { config [CONFIG_SECTION, "album-prefix-special", ""] = value; }
        }

        public static MediaShare CreateInstance (string configPath, FileSystems filesystems)
        {
            if (Instances.ContainsKey (configPath)) {
                return Instances [configPath];
            } else {
                return Instances [configPath] = new MediaShare (path: configPath, filesystems: filesystems);
            }
        }

        private MediaShare (string path, FileSystems filesystems)
            : base (path: path, configSection: CONFIG_SECTION)
        {
            fs = filesystems;

            int fuuuck = (GoogleAccount + SpecialAlbumPrefix).GetHashCode ();
            fuuuck++;

            AlbumMap = new Dictionary<string, Album> ();
            Media = new Dictionary<HexString, Medium> ();
            serializedAlbums = fs.Config.OpenConfigFile ("index_albums_" + Name + ".ini");
            serializedAlbums.AutoSaveEnabled = false;
            serializedMedia = fs.Config.OpenConfigFile ("index_media_" + Name + ".ini");
            serializedMedia.AutoSaveEnabled = false;
        }

        public string GoogleAccount {
            get { return config [CONFIG_SECTION, "google-account", "username"]; }
            set { config [CONFIG_SECTION, "google-account", "username"] = value; }
        }

        public void AddAlbum (Album album)
        {
            if (album != null) {
                AlbumMap [album.AlbumPath] = album;
            } else {
                throw new ArgumentNullException (string.Format ("Album is null: {0}", album));
            }
        }

        public void AddMedium (Medium media)
        {
            if (media != null) {
                Media [media.Hash] = media;
            } else {
                throw new ArgumentNullException (string.Format ("Media is null: {0}", media));
            }
        }

        public bool GetMediumByHash (HexString hash, out Medium medium)
        {
            if (Media.ContainsKey (hash)) {
                medium = Media [hash];
                return true;
            } else {
                medium = null;
                return false;
            }
        }

        public override string ToString ()
        {
            return string.Format ("PictureShare(Name=\"{0}\",RootDirectory=\"{1}\")", Name, RootDirectory);
        }

        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { Name };
        }

        public override bool Equals (object obj)
        {
            return ValueObject<MediaShare>.Equals (myself: this, obj: obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public static bool operator == (MediaShare a, MediaShare b)
        {
            return ValueObject<MediaShare>.Equality (a, b);
        }

        public static bool operator != (MediaShare a, MediaShare b)
        {
            return ValueObject<MediaShare>.Inequality (a, b);
        }

        public void UpdateIndex ()
        {
            NormalizeAlbumPaths ();

            // list media files
            Log.Message ("List media files...");
            Log.Indent++;
            Func<DirectoryInfo, bool> dirFilter = dir => !dir.Name.StartsWith (".");
            FileInfo[] pictureFiles = FileSystemLibrary.GetFileList (rootDirectory: RootDirectory, fileFilter: file => true, dirFilter: dirFilter, followSymlinks: true).ToArray ();

            // open progress bar
            ProgressBar progress = Log.OpenProgressBar (identifier: "PictureShare:" + RootDirectory, description: "Indexing media files...");
            int i = 0;
            int max = pictureFiles.Length;

            Log.Indent--;

            // index all media files
            Log.Message ("Index media files...");
            Log.Indent++;

            foreach (string _fullPath in from info in pictureFiles select info.FullName) {
                string fullPath = _fullPath;

                // something went horribly wrong!
                if (!fullPath.StartsWith (RootDirectory)) {
                    Log.Error ("[BUG] Invalid Path: fullPath=", fullPath, " is not in RootDirectory=", RootDirectory, "; stopped indexing.");
                    return;
                }

                if (!File.Exists (fullPath)) {
                    Log.Error ("File disappeared: ", fullPath);
                    continue;
                }

                // run index hooks. they may change the full path (and rename the file, obviously)
                MediaFile.RunIndexHooks (fullPath: ref fullPath);
                Picture.RunIndexHooks (fullPath: ref fullPath);
                Audio.RunIndexHooks (fullPath: ref fullPath);
                Video.RunIndexHooks (fullPath: ref fullPath);
                Document.RunIndexHooks (fullPath: ref fullPath);

                // create album, if necessery
                string albumPath = MediaShareUtilities.GetAlbumPath (fullPath: fullPath, share: this);
                Album album = AlbumMap.TryCreateEntry (key: albumPath, defaultValue: () => new Album (albumPath: albumPath, share: this));

                string relativePath = MediaShareUtilities.GetRelativePath (fullPath: fullPath, share: this);

                Func<MediaFile, bool> searchFile = file => file.FullPath == fullPath;
                // if the file has already been indexed
                if (album.ContainsFile (search: searchFile)) {
                    progress.Print (current: i, min: 0, max: max, currentDescription: "cached: " + relativePath, showETA: true, updateETA: false);

                    // check if some attributes may be missing
                    MediaFile cached;
                    album.GetFile (search: searchFile, result: out cached);
                    if (!cached.IsCompletelyIndexed) {
                        Log.Message ("Media file (update): ", fullPath);
                        Log.Indent++;

                        cached.Index ();

                        // put albums into global hashset
                        if (i % 100 == 0 || i >= max - 5) {
                            Serialize (verbose: false);
                        }

                        Log.Indent--;
                    }
                }
				// if the file needs to be indexed
				else if (MediaFile.IsValidFile (fullPath: fullPath)) {
                    progress.Print (current: i, min: 0, max: max, currentDescription: "indexing: " + relativePath, showETA: true, updateETA: true);
                    Log.Message ("Media file: ", fullPath);
                    Log.Indent++;

                    // create the file record and index it
                    MediaFile file = new MediaFile (fullPath: fullPath, share: this);
                    album.AddFile (file);

                    // put albums into global hashset
                    if (i % 50 == 0 || i >= max - 5) {
                        Serialize (verbose: false);
                    }

                    Log.Indent--;
                }
				// if the file is invalid or unknown
				else {
                    progress.Print (current: i, min: 0, max: max, currentDescription: "unknown: " + relativePath, showETA: true, updateETA: false);

                    if (!MediaFile.IsIgnoredFile (fullPath: fullPath)) {
                        Log.Message ("Unknown file: ", fullPath);
                    }
                }

                ++i;
            }

            progress.Finish ();
            Log.Indent--;

            // serialize
            Serialize (verbose: false);
        }

        private void NormalizeAlbumPaths ()
        {
            // list media files
            Log.Message ("Normalize media directories...");
            Log.Indent++;

            bool allPathsNormalized;
            do {
                allPathsNormalized = true;
                try {
                    DirectoryInfo[] pictureDirectories = FileSystemLibrary.GetDirectoryList (rootDirectory: RootDirectory, dirFilter: dir => true, followSymlinks: false, returnSymlinks: true).ToArray ();
                    foreach (string fullPath in from info in pictureDirectories select info.FullName) {
                        // Log.Debug (fullPath);
                        string albumPath = MediaShareUtilities.GetRelativePath (fullPath: fullPath, share: this);

                        // if the album name is not normalized
                        if (albumPath.Length > 0 && !NamingUtilities.IsNormalizedAlbumName (albumPath)) {
                            string newAlbumPath = NamingUtilities.NormalizeAlbumName (albumPath);
                            string newFullPath = Path.Combine (RootDirectory, newAlbumPath);

                            Log.Message ("Rename Album: ", fullPath, " => ", newFullPath);

                            if (Directory.Exists (newFullPath)) {
                                Log.Error ("Can't rename album: destination name already exists! (", newFullPath, ")");
                            } else {
                                // if the directory is a symlink
                                if (FileHelper.Instance.IsSymLink (fullPath)) {
                                    string target = FileHelper.Instance.ReadSymLink (fullPath);
                                    Log.Message ("Symlink (old): ", fullPath, " => ", target);
                                    Log.Message ("Symlink (new): ", newFullPath, " => ", target);
                                    FileHelper.Instance.CreateSymLink (target, newFullPath);
                                    File.Delete (fullPath);
                                    allPathsNormalized = false;
                                }
                                // or if it is a regular directory
                                else {
                                    Directory.Move (fullPath, newFullPath);
                                    allPathsNormalized = false;
                                }
                                break;
                            }
                        }

                        // if the album is a symlink whose target is not normalized
                        if (albumPath.Length > 0 && FileHelper.Instance.IsSymLink (fullPath)) {
                            string target = FileHelper.Instance.ReadSymLink (fullPath);
                            if (albumPath.Length > 0 && !NamingUtilities.IsNormalizedAlbumName (target)) {
                                string newTarget = NamingUtilities.NormalizeAlbumName (target);

                                Log.Message ("Change target of symlinked Album: ", fullPath);

                                Log.Message ("Symlink (old): ", fullPath, " => ", target);
                                Log.Message ("Symlink (new): ", fullPath, " => ", newTarget);
                                File.Delete (fullPath);
                                FileHelper.Instance.CreateSymLink (newTarget, fullPath);
                                allPathsNormalized = false;
                                break;
                            }
                        }
                    }
                } catch (IOException ex) {
                    Log.Error (ex);
                }
            } while (!allPathsNormalized);

            Log.Indent--;
        }

        public void CleanIndex ()
        {
            // clean up album list
            Log.Message ("Clean up index...");
            Log.Indent++;
            // iterate over a copy to be able to delete stuff
            foreach (Album album in Albums) {
                // iterate over a copy to be able to delete stuff
                foreach (MediaFile file in album.Files) {
                    if (!File.Exists (Path.Combine (RootDirectory, album.AlbumPath, file.Filename))) {
                        Log.Message ("File does not exist: ", file.RelativePath);
                        file.IsDeleted = true;
                    }
                }
                if (!Directory.Exists (Path.Combine (RootDirectory, album.AlbumPath))) {
                    Log.Message ("Album does not exist: ", album.AlbumPath);
                    album.IsDeleted = true;
                }
            }
            Serialize (verbose: false);
            Log.Indent--;
        }

        public void RebuildIndex (Filter albumFilter)
        {
            // clean up album list
            Log.Message ("Rebuild index...");
            Log.Indent++;
            RemoveFromIndex (albumFilter: albumFilter);
            UpdateIndex ();
            Log.Indent--;
        }

        public void RemoveFromIndex (Filter albumFilter)
        {
            // clean up album list
            Log.Message ("Remove from index...");
            Log.Indent++;
            foreach (Album album in Albums.Filter(albumFilter)) {
                Log.Message ("- ", album.AlbumPath);
                album.IsDeleted = true;
            }
            Serialize (verbose: false);
            Log.Indent--;
        }

        public void Deduplicate (Filter albumFilter)
        {
            PictureDeduplication dedup = new PictureDeduplication ();
            dedup.Deduplicate (share: this, albumFilter: albumFilter);
        }


        public void Serialize (bool verbose)
        {
            if (!Commons.CanStartPendingOperation) {
                Log.Error ("Can't serialize because we are exiting.");
                return;
            }

            if (verbose) {
                Log.Debug ("Serialize share: ", Name, ": albums... ");
            }
            

            Commons.PendingOperations++;
            foreach (Album album in Albums.ToArray()) {
                foreach (MediaFile file in album.Files.ToArray()) {
                    try {
                        if (file.IsDeleted) {
                            serializedAlbums.RemoveValue (section: album.AlbumPath, key: file.Filename);
                            album.RemoveFile (file);
                        } else {
                            serializedAlbums [section: album.AlbumPath, option: file.Filename, defaultValue: ""] = file.Medium.Hash.Hash;
                        }
                    } catch (Exception ex) {
                        Log.Error ("Error while serializing share: ", Name);
                        Log.Error ("In the albums loop, with album.AlbumPath=", album.AlbumPath, ", file.Name=", file.Filename, ", file.Medium=" + file.Medium);
                        Log.Error (ex);
                        album.RemoveFile (file);
                    }
                }
                if (album.IsDeleted) {
                    serializedAlbums.RemoveSection (section: album.AlbumPath);
                    AlbumMap.Remove (album.AlbumPath);
                }
            }

            if (verbose) {
                Log.Debug ("Serialize share: ", Name, ": media...");
            }
            

            foreach (Medium medium in Media.Values) {
                serializedMedia [section: medium.Hash.Hash, option: "type", defaultValue: ""] = medium.Type;
                Dictionary<string, string> dict = medium.Serialize ();
                foreach (KeyValuePair<string, string> entry in dict) {
                    serializedMedia [section: medium.Hash.Hash, option: entry.Key, defaultValue: ""] = entry.Value;
                }
            }

            serializedAlbums.Save ();
            serializedMedia.Save ();
            Commons.PendingOperations--;
        }

        public void Deserialize (bool verbose)
        {
            Media.Clear ();
            AlbumMap.Clear ();

            if (verbose) {
                Log.Debug ("Deserialize share: ", Name, ": media...");
            }

            foreach (string _hash in serializedMedia.Sections) {
                HexString hash = new HexString { Hash = _hash };
                string type = serializedMedia [section: _hash, option: "type", defaultValue: ""];
                Medium medium;

                if (type == Picture.TYPE) {
                    medium = new Picture (hash: hash);
                } else if (type == Audio.TYPE) {
                    medium = new Audio (hash: hash);
                } else if (type == Video.TYPE) {
                    medium = new Video (hash: hash);
                } else if (type == Document.TYPE) {
                    medium = new Document (hash: hash);
                } else {
                    Log.Error ("Error while deserializing share: ", Name, " (media)");
                    Log.Error ("Unknown type: " + type + " (hash: " + hash + ")");
                    continue;
                }
                Dictionary<string, string> dict = serializedMedia.SectionToDictionary (section: _hash);
                dict.Remove (key: "type");
                medium.Deserialize (dict);
                Media [medium.Hash] = medium;
            }

            if (verbose) {
                Log.Debug ("Deserialize share: ", Name, ": albums...");
            }

            foreach (string albumPath in serializedAlbums.Sections) {
                Album album = new Album (albumPath: albumPath, share: this);
                Dictionary<string, string> files = serializedAlbums.SectionToDictionary (section: albumPath);
                foreach (KeyValuePair<string, string> entry in files) {
                    string filename = entry.Key;
                    string fullPath = Path.Combine (RootDirectory, albumPath, filename);
                    HexString hash = new HexString { Hash = entry.Value };
                    MediaFile file = new MediaFile (fullPath: fullPath, hash: hash, share: this);
                    album.AddFile (mediaFile: file);
                }
                AlbumMap [album.AlbumPath] = album;
            }

            serializedAlbums.Save ();
            serializedMedia.Save ();
        }

        public void PrintAlbums (Filter albumFilter)
        {
            Log.Message (Name, " (in ", RootDirectory, "):");
            Log.Indent++;
            Log.Message (Albums.Filter (albumFilter).ToStringTable (
                a => LogColor.Reset,
                new[] { "Album", "Picture Files", "Audio Files", "Video Files", "Document Files" },
                a => a.AlbumPath,
                a => printCount (count: a.Files.Count (f => f.Medium is Picture), ifZero: "-"),
                a => printCount (count: a.Files.Count (f => f.Medium is Audio), ifZero: "-"),
                a => printCount (count: a.Files.Count (f => f.Medium is Video), ifZero: "-"),
                a => printCount (count: a.Files.Count (f => f.Medium is Document), ifZero: "-")
            ));
            Log.Indent--;
        }

        private string printCount (int count, string ifZero)
        {
            return count > 0 ? count + "" : ifZero;
        }
    }
}

