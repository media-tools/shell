using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Control.Common;
using Control.Common.IO;
using Control.Common.Tasks;

namespace Control.FileSync
{
    public class FileIndexTask : Task, MainTask
    {
        public FileIndexTask ()
        {
            Name = "FileIndex";
            ConfigName = "FileSync";
            Description = "Create an index of all files";
            Options = new string[] { "file-index", "fi" };
        }

        protected override void InternalRun (string[] args)
        {
            IEnumerable<FileInfo> files = FileSystemLibrary.GetFileList (rootDirectory: "/", fileFilter: file => true, dirFilter: dir => true);
            fs.Config.WriteAllLines(path: "index.txt", contents: from file in files select file.FullName);
        }
    }
}

