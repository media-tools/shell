using System;

namespace Shell.MailSync
{
    public class GoogleAccount : Account
    {
        public GoogleAccount (string accountname, string username, string password)
            : base (accountname: accountname, hostname: "imap.googlemail.com", username: username+"@gmail.com", password: password)
        {
        }

        public override string ToString ()
        {
            return "GoogleAccount(name=" + Accountname + ",user=" + Username.Split('@')[0] + ")"; //+ ",pass=" + String.Concat (Enumerable.Repeat ("?", Password.Length)) + ")";
        }
    }
}

