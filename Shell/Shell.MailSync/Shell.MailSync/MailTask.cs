using System;
using System.Linq;
using Mono.Options;
using Shell.Common;
using Shell.Common.Tasks;
using Shell.Namespaces;

namespace Shell.MailSync
{
    public class MailTask : MonoOptionsScriptTask, MainScriptTask
    {
        public MailTask ()
        {
            Name = "MailAll";
            Options = new [] { "mail" };
            ConfigName = NamespaceMail.CONFIG_NAME;

            Description = new [] {
                "Synchronize e-mails",
                "Show status of all e-mail folders"
            };
            Parameters = new [] {
                "sync",
                "status"
            };
            Methods = new Action[] {
                () => sync (),
                () => status (),
            };
        }

        protected override void SetupOptions (ref OptionSet optionSet)
        {
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

