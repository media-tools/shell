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
            Description = "Synchronize local directories";
            Options = new string[] { "file-sync", "fs" };
        }

        protected override void InternalRun (string[] args)
        {
            IEnumerable<FileInfo> files = FileSystemLibrary.GetFileList (rootDirectory: "/", filter: file => true);
            foreach (FileInfo file in files) {
                Log.MessageConsole ("  ", file.FullName);
            }
        }
    }
}

