using System;
using Control.Common;

namespace Control.MailSync
{
    public class MailSyncTask : Task
    {
        public MailSyncTask ()
        {
            Name = "MailSync";
            Description = "Synchronize e-mails";
            Options = new string[] { "mailsync" };
        }
    }
}
