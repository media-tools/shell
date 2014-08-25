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
            IEnumerable<FileInfo> files = FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: file => true, dirFilter: dir => true);
            List<Tree> trees = new List<Tree> ();
            foreach (FileInfo file in files) {
                if (file.Name == "control.ini") {
                    Log.MessageConsole ("  ", file.FullName);
                    trees.Add(new Tree (file.FullName));
                }
            }
        }
    }
}

