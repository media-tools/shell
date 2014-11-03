using System;
using System.Linq;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.MailSync
{
    public class MailTask : ScriptTask, MainScriptTask
    {
        public MailTask ()
        {
            Name = "MailAll";
            Description = new [] {
                "Synchronize e-mails",
                "Show status of all e-mail folders"
            };
            Options = new [] { "mail" };
            ParameterSyntax = new [] { "sync", "status" };
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "sync":
                    sync ();
                    break;
                case "status":
                    status ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
        }

        void status ()
        {
            MailLibrary lib = new MailLibrary (filesystems: fs);
            lib.UpdateConfigs ();
            MailSyncLibrary syncLib = new MailSyncLibrary (lib);
            syncLib.ListAccounts ();
            syncLib.ListFolders ();
        }

        void sync ()
        {
            MailLibrary lib = new MailLibrary (filesystems: fs);
            lib.UpdateConfigs ();
            MailSyncLibrary syncLib = new MailSyncLibrary (lib);
            syncLib.Sync ();
        }
    }
}

