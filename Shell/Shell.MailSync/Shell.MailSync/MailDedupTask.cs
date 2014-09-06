using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.MailSync
{
    public class MailDedupTask : ScriptTask, MainScriptTask
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

