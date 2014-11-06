using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common;
using Shell.Common.Tasks;
using Shell.Common.IO;

namespace Shell.Pictures
{
    public class PictureTask : ScriptTask, MainScriptTask
    {
        public PictureTask ()
        {
            Name = "Pictures";
            ConfigName = "Pictures";
            Description = new [] {
                "Update the picture index of all shares",
                "Update the picture index of all shares, and delete non-existant entries",
                "Find all picture shares"
            };
            Options = new [] { "pictures" };
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
            PictureShareManager shares = new PictureShareManager (rootDirectory: "/", filesystems: fs);
            shares.Initialize (cached: true);
            shares.Deserialize ();
            shares.Print ();
            shares.Index ();
            shares.Serialize ();
        }

        void clean ()
        {
            PictureShareManager shares = new PictureShareManager (rootDirectory: "/", filesystems: fs);
            shares.Initialize (cached: true);
            shares.Deserialize ();
            shares.Print ();
            shares.Clean ();
            shares.Serialize ();
        }

        void findShares ()
        {
            PictureShareManager shares = new PictureShareManager (rootDirectory: "/", filesystems: fs);
            shares.Initialize (cached: false);
            shares.Print ();
        }
    }
}

