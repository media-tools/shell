using System;
using Control.Common;

namespace Control.MailSync
{
    public class MailAllTask : Task
    {
        public MailAllTask ()
        {
            Name = "MailAll";
            Description = "Filter, synchronize and deduplicate e-mails";
            Options = new string[] { "mail-all" };
        }

        protected override void InternalRun (string[] args)
        {
            new MailFilterTask ().Run (args);
            new MailSyncTask ().Run (args);
            new MailDedupTask ().Run (args);
        }
    }
}

