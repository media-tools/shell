using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Options;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.GoogleSync.Core;
using Shell.Media;
using Shell.Media.Content;
using Shell.Namespaces;

namespace Shell.GoogleSync.Photos
{
    public class GooglePhotosTask : MonoOptionsScriptTask, MainScriptTask
    {
        public GooglePhotosTask ()
        {
            Name = "GooglePhotos";
            Options = new [] { "google-photos" };
            ConfigName = NamespaceGoogle.CONFIG_NAME;

            Description = new [] {
                "Find all picture shares on your hard disk",
                "Print the picture shares",
                "Print the local albums of the specified user",
                "Print the web albums of the specified user",
                "Print the photos in the specified web album.",
                "Upload local photos and videos",
                "Download photos und videos from auto backup that are not in any local album"
            };
            Parameters = new [] {
                "find-shares",
                "list-shares",
                "list-local-albums",
                "list-web-albums",
                "list-web-photos",
                "upload",
                "download-auto-backup",
            };
            Methods = new Action[] {
                () => findShares (),
                () => listShares (),
                () => listLocalAlbums (),
                () => listWebAlbums (),
                () => listWebPhotos (),
                () => upload (),
                () => downloadAutoBackup (),
            };
        }

        Type[] validTypes = new Type[] { typeof(Picture), typeof(Video) };
        Filter googleUserFilter = Filter.None;
        Filter shareFilter = Filter.None;
        Filter albumFilter = Filter.None;
        bool deleteExcess = false;

        protected override void SetupOptions (ref OptionSet optionSet)
        {
            optionSet = optionSet
                .Add ("only-photos",
                "Upload only photos",
                option => validTypes = new Type[] { typeof(Picture) })
                .Add ("only-videos",
                "Upload only videos",
                option => validTypes = new Type[] { typeof(Video) })
                .Add ("delete-excess",
                "Delete excess web files",
                option => deleteExcess = true)
                .Add ("user=",
                "Upload the share of the specified google user(s). Multiple values are seperated by comma.",
                option => googleUserFilter = Filter.ContainFilter (Filter.Split (option)))
                .Add ("share=",
                "Upload the share with the specified name(s). Multiple values are seperated by comma.",
                option => shareFilter = Filter.ContainFilter (Filter.Split (option)))
                .Add ("album=",
                "Upload only albums that begin with the specified prefix(es). Multiple values are seperated by comma.",
                option => albumFilter = Filter.ContainFilter (Filter.Split (option)))
                .Add ("debug-shares",
                "Show debug messages and errors regarding disabled shares",
                option => MediaShareManager.DEBUG_DISABLED_SHARES = true);
        }

        void listWebAlbums ()
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                if (googleUserFilter.Matches (acc)) {
                    Log.Message ("Google Account: ", acc);
                    Log.Indent++;
                    acc.Refresh ();
                    AlbumCollection albums = new AlbumCollection (account: acc);
                    albums.PrintAlbums (albumFilter: albumFilter);
                    Log.Indent--;
                }
            }
        }

        void listWebPhotos ()
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                if (googleUserFilter.Matches (acc)) {
                    Log.Message ("Google Account: ", acc);
                    Log.Indent++;
                    Log.Message ();
                    acc.Refresh ();

                    AlbumCollection albums = new AlbumCollection (account: acc);
                    foreach (WebAlbum album in albums.GetAlbums()) {
                        if (albumFilter.Matches (album)) {
                            Log.Message ("Album: ", album.Title);
                            Log.Indent++;
                            album.PrintPhotos ();
                            Log.Indent--;
                        }
                    }
                    Log.Indent--;
                }
            }
        }

        void upload ()
        {
            forMatchingShares ((share, webAlbumCollection, otherShares) => {
                webAlbumCollection.UploadShare (share: share, selectedTypes: validTypes, albumFilter: albumFilter, deleteExcess: deleteExcess);
            });
        }

        void downloadAutoBackup ()
        {
            forMatchingShares ((share, webAlbumCollection, otherShares) => {
                webAlbumCollection.DownloadAutoBackup (share: share, selectedTypes: validTypes, otherShares: otherShares);
            });
        }

        private void forMatchingShares (Action<MediaShare, AlbumCollection, MediaShare[]> todo)
        {
            MediaShareManager shareManager = new MediaShareManager (rootDirectory: "/");
            shareManager.Initialize (cached: true);
            shareManager.Deserialize ();

            GoogleShareManager googleShares = new GoogleShareManager (shareManager: shareManager);
            googleShares.PrintShares (shareFilter: shareFilter, googleUserFilter: googleUserFilter);
            MediaShare[] shares = googleShares.Shares;

            if (shares.Length != 0) {
                foreach (MediaShare share in shares.OrderBy (share => share.RootDirectory)) {
                    GoogleAccount account = googleShares [share];

                    // filter by google user!
                    if (!googleUserFilter.Matches (account)) {
                        Log.Debug ("Skip. (filtered by user filter: ", account, ")");
                        continue;
                    }
                    // filter by share name!
                    if (!shareFilter.Matches (share)) {
                        Log.Debug ("Skip. (filtered by share filter: ", share, ")");
                        continue;
                    }

                    Log.Message ("Share: ", share.Name, " (in ", share.RootDirectory, ")");
                    Log.Indent++;

                    Log.Message ("Google account: ", account);

                    account.Refresh ();
                    AlbumCollection webAlbumCollection = new AlbumCollection (account: account);

                    todo (share, webAlbumCollection, shareManager.Shares);

                    Log.Indent--;
                    Log.Message ();
                }
            } else {
                Log.Message ("No shares are available for uploading.");
            }
        }

        void findShares ()
        {
            MediaShareManager shareManager = new MediaShareManager (rootDirectory: "/");
            shareManager.Initialize (cached: false);
            shareManager.Deserialize ();

            GoogleShareManager googleShares = new GoogleShareManager (shareManager: shareManager);
            googleShares.PrintShares (shareFilter: shareFilter, googleUserFilter: googleUserFilter);
        }

        void listShares ()
        {
            MediaShareManager shareManager = new MediaShareManager (rootDirectory: "/");
            shareManager.Initialize (cached: true);
            shareManager.Deserialize ();

            GoogleShareManager googleShares = new GoogleShareManager (shareManager: shareManager);
            googleShares.PrintShares (shareFilter: shareFilter, googleUserFilter: googleUserFilter);
        }

        void listLocalAlbums ()
        {
            MediaShareManager shareManager = new MediaShareManager (rootDirectory: "/");
            shareManager.Initialize (cached: true);
            shareManager.Deserialize ();

            GoogleShareManager googleShares = new GoogleShareManager (shareManager: shareManager);
            googleShares.PrintAlbums (shareFilter: shareFilter, googleUserFilter: googleUserFilter, albumFilter: albumFilter);
        }
    }
}

