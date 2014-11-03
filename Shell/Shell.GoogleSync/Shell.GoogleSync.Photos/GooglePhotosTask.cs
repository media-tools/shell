using System;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.GoogleSync.Core;

namespace Shell.GoogleSync.Photos
{
    public class GooglePhotosTask : ScriptTask, MainScriptTask
    {
        public GooglePhotosTask ()
        {
            Name = "GooglePhotos";
            Description = new [] {
                "List the albums of all users",
                "List the photos of a specified album",
                "Configure google albums",
                "Synchronize google photos"
            };
            Options = new [] { "google-photos", "g-photos" };
            ConfigName = "Google";
            ParameterSyntax = new [] { "list-albums", "list-photos", "config", "sync" };
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "config":
                    config ();
                    break;
                case "list-albums":
                    listAlbums ();
                    break;
                case "list-photos":
                    listPhotos ();
                    break;
                case "sync":
                    sync ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
        }

        void config ()
        {
        }

        void listAlbums ()
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Message ("Google Account: ", acc);
                Log.Indent++;
                acc.Refresh ();
                AlbumCollection albums = new AlbumCollection (account: acc);
                albums.PrintAlbums ();
                Log.Indent--;
            }
        }

        void listPhotos ()
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Message ("Google Account: ", acc);
                Log.Indent++;
                Log.Message ();
                acc.Refresh ();
                AlbumCollection albums = new AlbumCollection (account: acc);
                foreach (WebAlbum album in albums.GetAlbums()) {
                    Log.Message ("Album: ", album.Title);
                    Log.Indent++;
                    album.PrintPhotos ();
                    Log.Indent--;
                }
                Log.Indent--;
            }
        }

        void sync ()
        {
        }
    }
}

