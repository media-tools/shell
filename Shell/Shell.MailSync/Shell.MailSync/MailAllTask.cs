using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.MailSync
{
    public class MailAllTask : ScriptTask, MainScriptTask
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

