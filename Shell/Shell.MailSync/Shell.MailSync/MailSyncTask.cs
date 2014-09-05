using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.MailSync
{
    public class MailSyncTask : Task, MainTask
    {
        public MailSyncTask ()
        {
            Name = "MailSync";
            Description = "Synchronize e-mails";
            Options = new string[] { "mail-sync" };
        }

        protected override void InternalRun (string[] args)
        {
            MailLibrary lib = new MailLibrary (filesystems: fs);
            lib.UpdateConfigs ();
            fs.Runtime.RequirePackages ("imapfilter", "isync");
            lib.RunMbsync ();
        }
    }
}

