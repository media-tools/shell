using System;
using System.Collections.Generic;
using System.IO;
using Control.Common;
using Control.Common.Tasks;
using Control.Common.IO;

namespace Control.FileSync
{
    public class FileIndexTask : Task, MainTask
    {
        public FileIndexTask ()
        {
            Name = "FileIndex";
            Description = "Create an index of all files";
            Options = new string[] { "file-index", "fi" };
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

