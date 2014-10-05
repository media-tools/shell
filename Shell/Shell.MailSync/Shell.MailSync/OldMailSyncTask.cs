using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.MailSync
{
    public class OldMailSyncTask : ScriptTask, MainScriptTask
    {
        public OldMailSyncTask ()
        {
            Name = "MailSync";
			Description = "Synchronize e-mails (via mbsync)";
            Options = new string[] { "mail-sync-old" };
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

