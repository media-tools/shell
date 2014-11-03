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
            Description = "Synchronize google photos";
            Options = new string[] { "google-photos", "g-photos" };
            ConfigName = "Google";
            ParameterSyntax = "list-albums | config | sync";
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

        void sync ()
        {
        }

        void error ()
        {
            Log.Error ("One of the following options is required: " + ParameterSyntax);
        }
    }
}

