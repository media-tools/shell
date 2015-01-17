using System;
using System.Collections.Generic;
using System.IO;
using Mono.Options;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Namespaces;

namespace Shell.Media
{
    public class MediaTask : MonoOptionsScriptTask, MainScriptTask
    {
        public MediaTask ()
        {
            Name = "Media";
            Options = new [] { "media" };
            ConfigName = NamespacePictures.CONFIG_NAME;

            Description = new [] {
                "Find all shares",
                "Update the media index.",
                "Delete non-existant media files in the index.",
                "Rebuild the media index for the specified album(s).",
                "Find duplicate media files in albums.",
                "Find duplicate media files in whole shares.",
                "Set the author to the value specified with '--author'. An album filter must be set.",
                "Install third-party executables",
                "Run garbage collection on the database",
            };
            Parameters = new [] {
                "find-shares",
                "index",
                "clean",
                "reindex",
                "deduplicate-albums",
                "deduplicate-shares",
                "set-author",
                "install",
                "gc",
            };
            Methods = new Action[] {
                () => findShares (),
                () => index (),
                () => clean (),
                () => reindex (),
                () => deduplicateAlbums (),
                () => deduplicateShares (),
                () => setAuthor (),
                () => install (),
                () => gc (),
            };
        }

        Filter shareFilter = Filter.None;
        Filter albumFilter = Filter.None;
        string author = string.Empty;
        bool dryRun = false;

        protected override void SetupOptions (ref OptionSet optionSet)
        {
            optionSet = optionSet
                .Add ("share=",
                "Only modify the specified share(s). Multiple names are seperated by comma.",
                option => shareFilter = Filter.ContainFilter (Filter.Split (option)))
                .Add ("album=",
                "Only modify the specified album(s). Multiple values are seperated by comma.",
                option => albumFilter = Filter.ContainFilter (Filter.Split (option)))
                .Add ("author=",
                "The author. Only use with '--set-author'!",
                option => author = option)
                .Add ("debug-shares",
                "Show debug messages and errors regarding disabled shares",
                option => MediaShareManager.DEBUG_DISABLED_SHARES = true)
                .Add ("dry-run",
                "Don't modify the file system",
                option => dryRun = option != null);
        }

        void index ()
        {
            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: true);
            shares.Deserialize ();
            // shares.Print ();
            shares.UpdateIndex (shareFilter: shareFilter);
            shares.Serialize ();
        }

        void clean ()
        {
            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: true);
            shares.Deserialize ();
            // shares.Print ();
            shares.CleanIndex (shareFilter: shareFilter);
            shares.Serialize ();
        }

        void reindex ()
        {
            if (albumFilter.AcceptsEverything) {
                Log.Error ("You have to specify an album filter!");
                return;
            }

            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: true);
            shares.Deserialize ();
            // shares.Print ();
            shares.RebuildIndex (shareFilter: shareFilter, albumFilter: albumFilter);
            shares.Serialize ();
        }

        void findShares ()
        {
            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: false);
            shares.Deserialize ();
            shares.PrintShares (shareFilter);
        }

        void deduplicateAlbums ()
        {
            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: true);
            shares.Deserialize ();
            shares.DeduplicateAlbums (shareFilter: shareFilter, albumFilter: albumFilter, dryRun: dryRun);
        }

        void deduplicateShares ()
        {
            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: true);
            shares.Deserialize ();
            shares.DeduplicateShares (shareFilter: shareFilter, albumFilter: albumFilter, dryRun: dryRun);
        }

        void setAuthor ()
        {
            if (string.IsNullOrWhiteSpace (author) || author != author.OnlyLetters ().ToLower ()) {
                Log.Error ("You have to set a valid author.");
                return;
            }

            if (albumFilter.AcceptsEverything || shareFilter.AcceptsEverything) {
                if (albumFilter.AcceptsEverything)
                    Log.Error ("You have to set a valid album filter.");
                if (shareFilter.AcceptsEverything)
                    Log.Error ("You have to set a valid share filter.");
                return;
            }

            albumFilter = Filter.ExactFilter (copyFrom: albumFilter);

            Log.Message ("Author: ", author);
            Log.Message ("Album filter: ", albumFilter);
            Log.Message ("Share filter: ", shareFilter);

            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: true);
            shares.Deserialize ();
            shares.SetAuthor (shareFilter: shareFilter, albumFilter: albumFilter, author: author, dryRun: dryRun);
        }

        void install ()
        {
            fs.Runtime.RequirePackages ("libimage-exiftool-perl");
        }

        void gc ()
        {
            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: true);
            shares.Deserialize ();
            shares.GarbageCollection ();
            shares.Serialize ();
        }
    }
}

