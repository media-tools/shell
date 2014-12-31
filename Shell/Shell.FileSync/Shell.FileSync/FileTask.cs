using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Options;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Namespaces;

namespace Shell.FileSync
{
    public class FileTask : MonoOptionsScriptTask, MainScriptTask
    {
        public FileTask ()
        {
            Name = "FileSync";
            Options = new [] { "files" };
            ConfigName = NamespaceFileSync.CONFIG_NAME;

            Description = new [] {
                "Find all local shares",
                "List the known local shares",
                "Synchronize the known local shares",
            };
            Parameters = new [] {
                "find-shares",
                "list-shares",
                "sync",
            };
            Methods = new Action[] {
                () => findShares (),
                () => listShares (),
                () => sync (),
            };
        }

        protected override void SetupOptions (ref OptionSet optionSet)
        {
            optionSet = optionSet
                .Add ("deep",
                "Deep sync (use hashs of file content instead of file size)",
                option => DataFile.DEEP_COMPARE = option != null);
        }

        void sync ()
        {
            ShareManager shares = new ShareManager (rootDirectory: "/");
            shares.Initialize (filesystems: fs, cached: true);
            shares.Print ();
            shares.Synchronize ();
        }

        void findShares ()
        {
            ShareManager shares = new ShareManager (rootDirectory: "/");
            shares.Initialize (filesystems: fs, cached: false);
            shares.Print ();
        }

        void listShares ()
        {
            ShareManager shares = new ShareManager (rootDirectory: "/");
            shares.Initialize (filesystems: fs, cached: true);
            shares.Print ();
        }
    }
}

