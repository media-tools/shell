using System;
using System.Collections.Generic;
using System.IO;
using Control.Common;
using Control.Common.Tasks;
using Control.Common.IO;

namespace Control.FileSync
{
    public class FileSyncTask : Task, MainTask
    {
        public FileSyncTask ()
        {
            Name = "FileSync";
            ConfigName = "FileSync";
            Description = "Synchronize local directories";
            Options = new string[] { "file-sync", "fs" };
        }

        protected override void InternalRun (string[] args)
        {
            ShareManager shares = new ShareManager (rootDirectory: "/");
            shares.Initialize ();
            shares.Print ();
            shares.Synchronize ();
        }
    }
}

