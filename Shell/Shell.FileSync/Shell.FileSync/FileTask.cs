using System;
using System.Collections.Generic;
using System.IO;
using Shell.Common;
using Shell.Common.Tasks;
using Shell.Common.IO;

namespace Shell.FileSync
{
    public class FileTask : ScriptTask, MainScriptTask
    {
        public FileTask ()
        {
            Name = "FileSync";
            ConfigName = "FileSync";
            Description = new [] {
                "Synchronize the known local shares",
                "Find all local shares",
                "List known local shares"
            };
            Options = new [] { "files" };
            ParameterSyntax = new [] { "sync", "find-shares", "list-shares" };
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "sync":
                    sync ();
                    break;
                case "find-shares":
                    findShares ();
                    break;
                case "list-shares":
                    listShares ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
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

