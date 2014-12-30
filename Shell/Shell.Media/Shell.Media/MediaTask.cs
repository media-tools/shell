using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Namespaces;

namespace Shell.Media
{
    public class MediaTask : ScriptTask, MainScriptTask
    {
        public MediaTask ()
        {
            Name = "Media";
            ConfigName = NamespacePictures.CONFIG_NAME;
            Description = new [] {
                "Update the picture index of all shares",
                "Update the picture index of all shares, and delete non-existant entries",
                "Find all picture shares"
            };
            Options = new [] { "media" };
            ParameterSyntax = new [] { "index", "clean", "find-shares" };
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "index":
                    index ();
                    break;
                case "clean":
                    clean ();
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

        void index ()
        {
            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: true);
            shares.Deserialize ();
            // shares.Print ();
            shares.Index (shareFilter: Filter.None);
            shares.Serialize ();
        }

        void clean ()
        {
            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: true);
            shares.Deserialize ();
            // shares.Print ();
            shares.Clean (shareFilter: Filter.None);
            shares.Serialize ();
        }

        void findShares ()
        {
            MediaShareManager shares = new MediaShareManager (rootDirectory: "/");
            shares.Initialize (cached: false);
            shares.Deserialize ();
            shares.PrintShares (Filter.None);
        }
    }
}

