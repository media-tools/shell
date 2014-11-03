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
            Description = new [] { "Create an index of all pictures", "Find all picture directories" };
            Options = new [] { "pictures" };
            ParameterSyntax = new [] { "index", "find-shares" };
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "index":
                    index ();
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
            shares.Print ();
            shares.Deserialize ();
            shares.Index ();
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

