using System;
using Control.Common;
using Control.Common.Tasks;

namespace Control.MailSync
{
    public class MailDedupTask : Task, MainTask
    {
        public MailDedupTask ()
        {
            Name = "MailDeduplicate";
            Description = "Deduplicate e-mails";
            Options = new string[] { "mail-dedup" };
        }

        protected override void InternalRun (string[] args)
        {
            MailLibrary lib = new MailLibrary (filesystems: fs);
            lib.UpdateConfigs ();
        }
    }
}

