using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Shares;
using Shell.Common.Util;

namespace Shell.FileSync
{
    public sealed class Tree : CommonShare<Tree>
    {
        private static string CONFIG_SECTION = "FileSync";

        public IEnumerable<DataFile> Files { get { return from file in FileMap.Keys
                                                                   select file; } }

        public bool IsSource { get; set; }

        public bool IsDestination { get; set; }

        private Dictionary<DataFile, DataFile> FileMap;

        public Tree (string path)
            : base (path: path, configSection: CONFIG_SECTION)
        {
        }

        public void CreateIndex ()
        {
            if (FileMap == null) {
                FileMap = new Dictionary<DataFile, DataFile> ();
                Func<FileInfo, bool> excludeTreeConfig = fileInfo => fileInfo.Name != CommonShare<Tree>.CONFIG_FILENAME;
                IEnumerable<FileInfo> files = FileSystemLibrary.GetFileList (rootDirectory: RootDirectory, fileFilter: excludeTreeConfig, dirFilter: dir => true, symlinks: false);
                foreach (FileInfo fileInfo in files) {
                    DataFile file = new DataFile (fileInfo: fileInfo, tree: this);
                    FileMap [file] = file;
                }
            }
            
            Log.DebugLog ("Index of: ", RootDirectory);
            Log.Indent++;
            foreach (DataFile file in Files) {

                Log.DebugLog ("- ", file);
            }
            Log.Indent--;
        }

        public bool ContainsFile (DataFile search, out DataFile result)
        {
            if (FileMap.ContainsKey (search)) {
                result = FileMap [search];
                return true;
            } else {
                result = null;
                return false;
            }
        }

        public override string ToString ()
        {
            return string.Format ("Tree(name=\"{0}\", rootDirectory=\"{1}\", enabled={2}, read={3}, write={4}, delete={5})", Name, RootDirectory, IsEnabled, IsReadable, IsWriteable, IsDeletable);
        }

        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { ToString () };
        }

        public override bool Equals (object obj)
        {
            return ValueObject<Tree>.Equals (myself: this, obj: obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public static bool operator == (Tree a, Tree b)
        {
            return ValueObject<Tree>.Equality (a, b);
        }

        public static bool operator != (Tree a, Tree b)
        {
            return ValueObject<Tree>.Inequality (a, b);
        }
    }
}

