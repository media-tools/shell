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
    public class PictureShare
    {
        private static Dictionary<string, PictureShare> Instances = new Dictionary<string, PictureShare> ();

        public static string PICTURE_CONFIG_FILENAME = "control.ini";
        private static string CONFIG_SECTION = "Pictures";

        public string ConfigPath { get; private set; }

        public string RootDirectory { get; private set; }

        public HashSet<Album> Albums { get; private set; }

        public HashSet<Media> Media { get; private set; }

        private ConfigFile config;

        public static PictureShare CreateInstance (string configPath)
        {
            if (Instances.ContainsKey (configPath)) {
                return Instances [configPath];
            } else {
                return Instances [configPath] = new PictureShare (path: configPath);
            }
        }

        private PictureShare (string path)
        {
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
            Media = new HashSet<Media> ();
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

        public void Add (Media media)
        {
            if (media != null) {
                Media.Add (media);
            } else {
                throw new ArgumentNullException (string.Format ("Media is null: {0}", media));
            }
        }

        public bool GetMediaByHash (HexString hash, out Media media)
        {
            IEnumerable<Media> cached = Media.Where (m => m.Hash == hash);
            if (cached.Count () == 1) {
                media = cached.First ();
                return true;
            } else {
                media = null;
                return false;
            }
        }

        public override string ToString ()
        {
            return string.Format ("PictureShare(Name=\"{0}\",RootDirectory=\"{1}\")", Name, RootDirectory);
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode ();
        }

        public override bool Equals (object obj)
        {
            return Equals (obj as PictureShare);
        }

        public bool Equals (PictureShare obj)
        {
            return obj != null && GetHashCode () == obj.GetHashCode ();
        }

        private static string RandomString ()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToLower ();
            var random = new Random ();
            var result = new string (Enumerable.Repeat (chars, 8).Select (s => s [random.Next (s.Length)]).ToArray ());
            return result;
        }

        public void Index (FileSystems filesystems)
        {
            IEnumerable<FileInfo> pictureFiles = Shell.FileSync.FileSystemLibrary.GetFileList (rootDirectory: RootDirectory, fileFilter: file => true, dirFilter: dir => true);
            Dictionary<string, Album> albums = new Dictionary<string, Album> (); 
            foreach (FileInfo info in pictureFiles) {
                if (info.FullName.StartsWith (RootDirectory)) {
                    if (MediaFile.IsValidFile (fullPath: info.FullName)) {
                        MediaFile file = new MediaFile (fullPath: info.FullName, share: this);
                        if (!albums.ContainsKey (file.AlbumPath)) {
                            albums [file.AlbumPath] = new Album (albumPath: file.AlbumPath);
                        }
                        albums [file.AlbumPath].Add (file);
                    } else {
                        Log.Debug ("Unknown file: ", info.FullName);
                    }
                } else {
                    Log.Error ("Invalid Path: info.FullName=", info.FullName, " is not in RootDirectory=", RootDirectory);
                    return;
                }
            }

            Albums = albums.Values.ToHashSet ();
        }

        private string FILENAME_ALBUMS { get { return "index_albums_" + Name + ".json"; } }

        private string FILENAME_MEDIA { get { return "index_media_" + Name + ".json"; } }

        public void Serialize (FileSystems filesystems)
        {
            // save the albums
            HexString hash1 = filesystems.Config.HashOfFile (name: FILENAME_ALBUMS);
            filesystems.Config.Serialize<Album> (path: FILENAME_ALBUMS, enumerable: Albums);

            // deserialize and serialize
            List<Album> deserialized;
            filesystems.Config.Deserialize<Album> (path: FILENAME_ALBUMS, list: out deserialized);
            filesystems.Config.Serialize<Album> (path: FILENAME_ALBUMS, enumerable: deserialized);
            HexString hash2 = filesystems.Config.HashOfFile (name: FILENAME_ALBUMS);

            // check if it's the same
            if (hash1 != hash2) {
                Log.Error ("Bug in MediaFileConverter! serialized deserialized index is not the same!");
            }
        }

        public void Deserialize (FileSystems filesystems)
        {
            List<Album> deserialized;
            filesystems.Config.Deserialize<Album> (path: FILENAME_ALBUMS, list: out deserialized);
            Albums = deserialized.ToHashSet ();
        }

        public void Sort ()
        {
            throw new NotImplementedException ();
        }
    }
}

