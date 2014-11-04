using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.GoogleSync.Core;
using Shell.Pictures;

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
            ParameterSyntax = new [] { "list-albums", "list-photos", "config", "upload" };
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
                case "upload":
                    upload ();
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

        void upload ()
        {
            PictureShareManager shares = new PictureShareManager (rootDirectory: "/", filesystems: fs);
            shares.Initialize (cached: true);
            shares.Print ();
            shares.Deserialize ();

            GoogleAccount[] googleAccounts = GoogleAccount.List ().ToArray ();

            if (shares.PictureDirectories.Count != 0) {
                // for all shares
                foreach (PictureShare share in from share in shares.PictureDirectories.Values orderby share.RootDirectory select share) {
                    Log.Message ("Share: ", share);

                    // if there is a valid google account config value
                    if (!string.IsNullOrWhiteSpace (share.GoogleAccount)) {
                        GoogleAccount[] matches = googleAccounts.Where (a => a.Emails.Replace (".", "").ToLower ().Contains (share.GoogleAccount)).ToArray ();

                        // if there are no google accounts matching the value
                        if (matches.Length == 0) {
                            Log.Message ("No google accounts match this share.");
                        }
                        // if there are more than one matching google accounts 
                        else if (matches.Length >= 2) {
                            Log.Message ("More than one google account match this share: ", string.Join (", ", matches.Select (a => a.DisplayName + " <" + a.Emails + ">")));
                        }
                        // if there is exactly one matching google account!
                        else {
                            GoogleAccount account = matches [0];

                            Log.Message ("One google account matches the share: ", account);
                            Log.Indent++;
                            Log.Message ();
                            account.Refresh ();
                            uploadShare (share, account);
                            Log.Indent--;
                        }
                    }
                }
            } else {
                Log.Message ("No shares are available for uploading.");
            }

            shares.Serialize ();
        }

        void uploadShare (PictureShare share, GoogleAccount account)
        {
            // load web albums
            Log.Message ("Load web albums: ");
            Log.Indent++;
            Dictionary<WebAlbum, WebPhoto[]> webAlbums = new Dictionary<WebAlbum, WebPhoto[]> ();
            AlbumCollection webAlbumCollection = new AlbumCollection (account: account);
            foreach (WebAlbum album in webAlbumCollection.GetAlbums()) {
                webAlbums [album] = webAlbumCollection.GetPhotos (album);
                Log.Message ("- ", album.Title);
            }
            Log.Indent--;

            // compare local and web albums...
            Log.Message ("Compare local and web albums...");
            Log.Indent++;


            Log.Indent--;
        }
    }
}

