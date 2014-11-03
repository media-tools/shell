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
                Albums albums = new Albums (account: acc);
                albums.PrintAllAlbums ();
                Log.Indent--;
            }
        }

        void listPhotos ()
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Message ("Google Account: ", acc);
                Log.Indent++;
                acc.Refresh ();
                Albums albums = new Albums (account: acc);
                albums.PrintAllAlbums ();
                Log.Indent--;
            }
        }

        void sync ()
        {
        }

        void error ()
        {
            Log.Error ("One of the following options is required: " + ParameterSyntax);
        }
    }
}

