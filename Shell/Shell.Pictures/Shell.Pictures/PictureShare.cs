using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using System.Security.Cryptography;
using Shell.Pictures.Files;
using Shell.Pictures.Content;

namespace Shell.Pictures
{
    public class PictureShare : ValueObject<PictureShare>
    {
        private static Dictionary<string, PictureShare> Instances = new Dictionary<string, PictureShare> ();

        public static string PICTURE_CONFIG_FILENAME = "control.ini";
        private static string CONFIG_SECTION = "Pictures";

        public string ConfigPath { get; private set; }

        public string RootDirectory { get; private set; }

        public HashSet<Album> Albums { get; private set; }

        public HashSet<Medium> Media { get; private set; }

        private ConfigFile config;
        private ConfigFile serializedAlbums;
        private ConfigFile serializedMedia;

        private FileSystems fs;

        public static PictureShare CreateInstance (string configPath, FileSystems filesystems)
        {
            if (Instances.ContainsKey (configPath)) {
                return Instances [configPath];
            } else {
                return Instances [configPath] = new PictureShare (path: configPath, filesystems: filesystems);
            }
        }

        private PictureShare (string path, FileSystems filesystems)
        {
            fs = filesystems;
            if (Path.GetFileName (path) == PICTURE_CONFIG_FILENAME) {
                RootDirectory = Path.GetDirectoryName (path);
                ConfigPath = path;
            } else {
                throw new ArgumentException ("Illegal tree config file: " + path);
            }

            config = new ConfigFile (filename: ConfigPath);
            int fuuuck = (Name + IsEnabled + IsWriteable + IsExperimental).GetHashCode ();
            fuuuck++;

            if (Commons.IS_EXPERIMENTAL != IsExperimental) {
                throw new ArgumentException ("Can't use that in " + (Commons.IS_EXPERIMENTAL ? "" : "not ") + "experimental mode: " + path);
            }

            Albums = new HashSet<Album> ();
            Media = new HashSet<Medium> ();
            serializedAlbums = fs.Config.OpenConfigFile ("index_albums_" + Name + ".ini");
            serializedMedia = fs.Config.OpenConfigFile ("index_media_" + Name + ".ini");
        }

        public string Name {
            get { return config [CONFIG_SECTION, "name", RandomString ()]; }
            set { config [CONFIG_SECTION, "name", RandomString ()] = value; }
        }

        public bool IsEnabled {
            get { return config [CONFIG_SECTION, "enabled", false]; }
            set { config [CONFIG_SECTION, "enabled", false] = value; }
        }

        public bool IsWriteable {
            get { return config [CONFIG_SECTION, "write", true]; }
            set { config [CONFIG_SECTION, "write", true] = value; }
        }

        public bool IsDeletable {
            get { return config [CONFIG_SECTION, "delete", false]; }
            set { config [CONFIG_SECTION, "delete", false] = value; }
        }

        public bool IsExperimental {
            get { return config [CONFIG_SECTION, "experimental", false]; }
            set { config [CONFIG_SECTION, "experimental", false] = value; }
        }

        public void Add (Album album)
        {
            if (album != null) {
                Albums.Add (album);
            } else {
                throw new ArgumentNullException (string.Format ("Album is null: {0}", album));
            }
        }

        public void Add (Medium media)
        {
            if (media != null) {
                Media.Add (media);
            } else {
                throw new ArgumentNullException (string.Format ("Media is null: {0}", media));
            }
        }

        public bool GetMediumByHash (HexString hash, out Medium medium)
        {
            IEnumerable<Medium> cached = Media.Where (m => m.Hash == hash);
            if (cached.Count () == 1) {
                medium = cached.First ();
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
            return ValueObject<PictureShare>.Equals (myself: this, obj: obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public static bool operator == (PictureShare a, PictureShare b)
        {
            return ValueObject<PictureShare>.Equality (a, b);
        }

        public static bool operator != (PictureShare a, PictureShare b)
        {
            return ValueObject<PictureShare>.Inequality (a, b);
        }

        private static string RandomString ()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToLower ();
            var random = new Random ();
            var result = new string (Enumerable.Repeat (chars, 8).Select (s => s [random.Next (s.Length)]).ToArray ());
            return result;
        }

        public void Index ()
        {
            IEnumerable<FileInfo> pictureFiles = Shell.FileSync.FileSystemLibrary.GetFileList (rootDirectory: RootDirectory, fileFilter: file => true, dirFilter: dir => true);
            Dictionary<string, Album> albums = new Dictionary<string, Album> (); 
            foreach (string fullPath in from info in pictureFiles select info.FullName) {

                if (!fullPath.StartsWith (RootDirectory)) {
                    Log.Error ("[BUG] Invalid Path: fullPath=", fullPath, " is not in RootDirectory=", RootDirectory, "; stopped indexing.");
                    return;
                }

                string albumPath = PictureShareUtilities.GetAlbumPath (fullPath: fullPath, share: this);
                Album album = albums.TryCreateEntry (key: albumPath, defaultValue: () => new Album (albumPath: albumPath), onValueCreated: a => Albums.Add (a));

                if (album.Contains (search: file => file.FullPath == fullPath)) {
                    Log.Debug ("Media file (cached): ", fullPath);
                } else if (MediaFile.IsValidFile (fullPath: fullPath)) {
                    Log.Debug ("Media file: ", fullPath);
                    MediaFile file = new MediaFile (fullPath: fullPath, share: this);
                    file.Index (album: album);
                    album.Add (file);

                    Serialize ();
                } else {
                    Log.Debug ("Unknown file: ", fullPath);
                }
            }

            Albums = albums.Values.ToHashSet ();
        }

        public void Serialize ()
        {
            if (!Commons.CanStartPendingOperation) {
                Log.Error ("Can't serialize because we are exiting.");
                return;
            }

            Commons.PendingOperations++;
            foreach (Album album in Albums) {
                foreach (MediaFile file in album.Files) {
                    serializedAlbums [section: album.AlbumPath, option: file.Name, defaultValue: ""] = file.Medium.Hash.Hash;
                }
            }
            foreach (Medium medium in Media) {
                serializedMedia [section: medium.Hash.Hash, option: "type", defaultValue: ""] = medium.Type;
                Dictionary<string, string> dict = medium.Serialize ();
                foreach (KeyValuePair<string, string> entry in dict) {
                    serializedMedia [section: medium.Hash.Hash, option: entry.Key, defaultValue: ""] = entry.Value;
                }
            }
            Commons.PendingOperations--;
        }

        public void Deserialize ()
        {
            Media.Clear ();
            Albums.Clear ();

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
                } else {
                    Log.Error ("Unknown type: " + type + " (hash: " + hash + ")");
                    continue;
                }
                Dictionary<string, string> dict = serializedMedia.SectionToDictionary (section: _hash);
                dict.Remove (key: "type");
                medium.Deserialize (dict);
                Media.Add (medium);
            }

            foreach (string albumPath in serializedAlbums.Sections) {
                Album album = new Album (albumPath: albumPath);
                Dictionary<string, string> files = serializedAlbums.SectionToDictionary (section: albumPath);
                foreach (KeyValuePair<string, string> entry in files) {
                    string filename = entry.Key;
                    string fullPath = Path.Combine (RootDirectory, albumPath, filename);
                    HexString hash = new HexString { Hash = entry.Value };
                    MediaFile file = new MediaFile (fullPath: fullPath, hash: hash, share: this);
                    album.Add (mediaFile: file);
                }
                Albums.Add (album);
            }
        }

        public void Sort ()
        {
            throw new NotImplementedException ();
        }
    }
}

