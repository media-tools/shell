using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common;
using Shell.Common.Tasks;
using Shell.Common.IO;

namespace Shell.FileSync
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
            shares.Initialize (filesystems: fs, cached: true);
            shares.Print ();
            shares.Synchronize ();
        }
    }
}

