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
            Description = new [] { "Create an index of all files", "Synchronize local directories" };
            Options = new [] { "files" };
            ParameterSyntax = new [] { "index", "sync" };
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "index":
                    index ();
                    break;
                case "sync":
                    sync ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
        }

        void index ()
        {
            ShareManager shares = new ShareManager (rootDirectory: "/");
            shares.Initialize (filesystems: fs, cached: false);
            shares.Print ();
        }

        void sync ()
        {
            ShareManager shares = new ShareManager (rootDirectory: "/");
            shares.Initialize (filesystems: fs, cached: true);
            shares.Print ();
            shares.Synchronize ();
        }
    }
}

