using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.GoogleSync.Core;
using Shell.Pictures;
using Shell.Pictures.Content;

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
                "Upload local photos and videos to google albums",
                "Upload local photos to google albums",
                "Upload local videos to google albums",
                "Find all picture directories"
            };
            Options = new [] { "google-photos", "g-photos" };
            ConfigName = "Google";
            ParameterSyntax = new [] {
                "list-albums",
                "list-photos",
                "config",
                "upload",
                "upload-photos",
                "upload-videos",
                "find-shares"
            };
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
                    upload (typeof(Picture), typeof(Video));
                    break;
                case "upload-photos":
                    upload (typeof(Picture));
                    break;
                case "upload-videos":
                    upload (typeof(Video));
                    break;
                case "find-shares":
                    findShares ();
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

        void upload (params Type[] validTypes)
        {
            PictureShareManager shares = new PictureShareManager (rootDirectory: "/", filesystems: new PictureDummyLibrary ().FileSystems);
            shares.Initialize (cached: true);
            shares.Deserialize ();

            GoogleAccount[] googleAccounts = GoogleAccount.List ().ToArray ();

            if (shares.PictureDirectories.Count != 0) {
                // for all shares. ~/.bashrc
                
                foreach (PictureShare share in from share in shares.PictureDirectories.Values orderby share.RootDirectory select share) {
                    Log.Message ();
                    Log.Message ("Share: ", share);
                    Log.Indent++;
                    Log.Message ();

                    // if there is a valid google account config value
                    if (!string.IsNullOrWhiteSpace (share.GoogleAccount)) {
                        GoogleAccount[] matches = googleAccounts.Where (a => a.Emails.Replace (".", "").ToLower ().Contains (share.GoogleAccount.Replace (".", "").ToLower ())).ToArray ();

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
                            account.Refresh ();
                            AlbumCollection webAlbumCollection = new AlbumCollection (account: account);
                            webAlbumCollection.UploadShare (share: share, selectedTypes: validTypes);
                        }
                    }

                    Log.Indent--;
                    Log.Message ();
                }
            } else {
                Log.Message ("No shares are available for uploading.");
            }

            shares.Serialize ();
        }

        void findShares ()
        {
            PictureShareManager shares = new PictureShareManager (rootDirectory: "/", filesystems: new PictureDummyLibrary ().FileSystems);
            shares.Initialize (cached: false);
            shares.Print ();
        }

        protected class PictureDummyLibrary : Library
        {
            public PictureDummyLibrary ()
            {
                ConfigName = "Pictures";
            }
        }
    }
}

