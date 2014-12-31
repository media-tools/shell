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
            };
            Parameters = new [] {
                "find-shares",
                "index",
                "clean",
                "reindex",
            };
            Methods = new Action[] {
                () => findShares (),
                () => index (),
                () => clean (),
                () => reindex (),
            };
        }

        Filter shareFilter = Filter.None;
        Filter albumFilter = Filter.None;

        protected override void SetupOptions (ref OptionSet optionSet)
        {
            optionSet = optionSet
                .Add ("share=",
                "Only modify the specified share(s). Multiple names are seperated by ',' or ';'.",
                option => shareFilter = Filter.ContainFilter (Filter.Split (option)))
                .Add ("album=",
                "Only modify the specified album(s). Multiple values are seperated by comma.",
                option => albumFilter = Filter.ContainFilter (Filter.Split (option)))
                .Add ("debug-shares",
                "Show debug messages and errors regarding disabled shares",
                option => MediaShareManager.DEBUG_DISABLED_SHARES = true);
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
    }
}

