using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common;
using Shell.Common.Tasks;
using Shell.Common.IO;

namespace Shell.Pictures
{
    public class PictureIndexTask : ScriptTask, MainScriptTask
    {
        public PictureIndexTask ()
        {
            Name = "PictureIndex";
            ConfigName = "Pictures";
            Description = "Create an index of all pictures";
            Options = new string[] { "picture-index", "p-i" };
        }

        protected override void InternalRun (string[] args)
        {
            PictureShareManager shares = new PictureShareManager (rootDirectory: "/");
            shares.Initialize (filesystems: fs, cached: true);
            shares.Print ();
            shares.Index (filesystems: fs);
        }
    }
}

