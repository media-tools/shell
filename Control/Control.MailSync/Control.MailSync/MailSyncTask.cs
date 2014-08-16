using System;
using Control.Common;
using System.Collections.Generic;

namespace Control.MailSync
{
    public class MailSyncTask : Task
    {
        private List<Account> accounts = new List<Account> ();

        public MailSyncTask ()
        {
            Name = "MailSync";
            Description = "Synchronize e-mails";
            Options = new string[] { "mailsync" };
        }

        protected override void InternalRun ()
        {
            readConfig ();
            writeConfigs ();

            fs.Runtime.RequirePackages ("imapfilter", "isync");
            writeScripts ();
            runScripts ();
        }

        private void readConfig ()
        {
            if (!fs.Config.Exists (path: ACCOUNTS_CONF)) {
                fs.Config.WriteAllText (path: ACCOUNTS_CONF, contents: "google ACCOUT_NAME USER_NAME PASSWORD");
            }
            string[] lines = fs.Config.ReadAllLines (path: ACCOUNTS_CONF);
            int lineNum = 1;
            foreach (string line in lines) {
                string[] parts = line.Split (new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1 && parts [0] == "google") {
                    if (parts.Length == 4) {
                        Account acc = new GoogleAccount (accountname: parts [1], username: parts [2], password: parts [3]);
                        accounts.Add (acc);
                        Log.Message ("Found account: ", acc);
                    } else {
                        Log.Message ("Invalid google account in ", ACCOUNTS_CONF, ":", lineNum);
                    }
                } else if (parts.Length > 0) {
                    Log.Message ("Invalid account in ", ACCOUNTS_CONF, ":", lineNum);
                }
                ++lineNum;
            }
        }

        private void writeConfigs ()
        {
            string mbsyncrc = "";
            foreach (Account acc in accounts) {
                mbsyncrc += "IMAPAccount " + acc.Accountname + "\nHost " + acc.Hostname + "\nUser " + acc.Username + "\nPass " + acc.Password + "\nUseIMAPS yes\n"
                    + "CertificateFile /etc/ssl/certs/ca-certificates.crt\n\n" + "IMAPStore " + acc.Accountname + "-imap\n"
                    + "Account " + acc.Accountname + "\n\n";
            }
            fs.Runtime.WriteAllText (path: MBSYNC_RC, contents: mbsyncrc);
        }

        private void writeScripts ()
        {
            fs.Runtime.WriteAllText (path: "run1.sh", contents: CONTENT_BIN_RUN1);
            fs.Runtime.WriteAllText (path: "run2.sh", contents: CONTENT_BIN_RUN2);
        }

        private void runScripts ()
        {
            fs.Runtime.ExecuteScript (path: "run1.sh");
            fs.Runtime.ExecuteScript (path: "run2.sh");
        }
        
        private string ACCOUNTS_CONF = "accounts.conf";
        private string IMAPFILTER_LUA = "imapfilter.lua";
        private string MBSYNC_RC = "mbsyncrc";

        private string CONTENT_BIN_RUN1 { get { return "imapfilter -v -c " + fs.Runtime.RootDirectory + SystemInfo.PathSeparator + IMAPFILTER_LUA; } }

        private string CONTENT_BIN_RUN2 { get { return "mbsync -c " + fs.Runtime.RootDirectory + SystemInfo.PathSeparator + MBSYNC_RC + " -a"; } }
    }

}
