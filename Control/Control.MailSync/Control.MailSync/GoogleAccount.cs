using System;

namespace Control.MailSync
{
    public class GoogleAccount : Account
    {
        public GoogleAccount (string accountname, string username, string password)
            : base (accountname: accountname, hostname: "imap.googlemail.com", username: username+"@gmail.com", password: password)
        {
        }
    }
}

