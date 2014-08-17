using System;
using Control.Common;

namespace Control.MailSync
{
    public class MailFilterTask : Task
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

