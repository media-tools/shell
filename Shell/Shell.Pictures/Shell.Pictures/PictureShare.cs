using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;

namespace Shell.Pictures
{
    public class PictureShare
    {
        public static string PICTURE_CONFIG_FILENAME = "control.ini";
        private static string CONFIG_SECTION = "Pictures";

        public string ConfigPath { get; private set; }

        public string RootDirectory { get; private set; }

        public HashSet<Album> Albums { get; private set; }

        private ConfigFile config;

        public PictureShare (string path)
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
                throw new ArgumentNullException (string.Format ("Invalid albums: {0}", album));
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
                    MediaFile file = null;
                    if (PictureFile.IsValidFile (fileInfo: info)) {
                        file = new PictureFile (fullPath: info.FullName, root: RootDirectory);
                    } else if (VideoFile.IsValidFile (fileInfo: info)) {
                        file = new VideoFile (fullPath: info.FullName, root: RootDirectory);
                    } else if (AudioFile.IsValidFile (fileInfo: info)) {
                        file = new AudioFile (fullPath: info.FullName, root: RootDirectory);
                    } else {
                        Log.Debug ("Unknown file: ", info.FullName);
                    }
                    if (file != null) {
                        if (!albums.ContainsKey (file.AlbumPath)) {
                            albums [file.AlbumPath] = new Album (albumPath: file.AlbumPath);
                        }
                        albums [file.AlbumPath].Add (file);
                    }
                } else {
                    Log.Error ("Invalid Path: info.FullName=", info.FullName, " is not in RootDirectory=", RootDirectory);
                    return;
                }
            }
            filesystems.Config.Serialize<Album> (path: "index-"+Name+".json", enumerable: albums.Values);
            List<Album> deserialized;
            filesystems.Config.Deserialize<Album>(path: "index-"+Name+".json", list: out deserialized);
            filesystems.Config.Serialize<Album> (path: "index-"+Name+".json2", enumerable: deserialized);
            
        }

        public void Sort ()
        {
            throw new NotImplementedException ();
        }
    }
}

