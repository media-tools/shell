using System;
using Shell.Common;
using Shell.Common.Tasks;

namespace Shell.MailSync
{
    public class MailFilterTask : Task, MainTask
    {
        public MailFilterTask ()
        {
            Name = "MailFilter";
            Description = "Filter e-mails";
            Options = new string[] { "mail-filter" };
        }

        protected override void InternalRun (string[] args)
        {
            MailLibrary lib = new MailLibrary (filesystems: fs);
            lib.UpdateConfigs ();
            fs.Runtime.RequirePackages ("imapfilter");
            lib.RunImapFilter ();
        }
    }
}

