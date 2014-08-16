using System;
using Control.Common;
using System.Collections.Generic;

namespace Control.MailSync
{
    public class MailSyncTask : Task
    {
        private List<Account> accounts = new List<Account> ();
        private List<Channel> channels = new List<Channel> ();

        public MailSyncTask ()
        {
            Name = "MailSync";
            Description = "Synchronize e-mails";
            Options = new string[] { "mailsync" };
        }

        protected override void InternalRun ()
        {
            readConfigs ();
            writeConfigs ();

            fs.Runtime.RequirePackages ("imapfilter", "isync");
            writeScripts ();
            runScripts ();
        }

        private void readConfigs ()
        {
            readConfigAccounts ();
            readConfigChannels ();
        }

        private void readConfigAccounts ()
        {
            if (!fs.Config.FileExists (path: ACCOUNTS_CONF)) {
                fs.Config.WriteAllText (path: ACCOUNTS_CONF, contents: "google ACCOUT_NAME_1 USER_NAME PASSWORD");
                fs.Config.WriteAllText (path: ACCOUNTS_CONF, contents: "google ACCOUT_NAME_2 USER_NAME PASSWORD");
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

        private void readConfigChannels ()
        {
            if (!fs.Config.FileExists (path: CHANNELS_CONF)) {
                fs.Config.WriteAllText (path: CHANNELS_CONF, contents: "ACCOUT_NAME_1:PATH -> ACCOUT_NAME_2:PATH");
            }
            string[] lines = fs.Config.ReadAllLines (path: CHANNELS_CONF);
            int lineNum = 1;
            foreach (string line in lines) {
                string[] parts = line.Split (new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2) {
                    try {
                        parts [0] = parts [0].Trim ('\t', ' ');
                        parts [1] = parts [1].Trim ('\t', ' ');
                        Channel chan = new Channel (accounts: accounts, from: parts [0], to: parts [1]);
                        channels.Add (chan);
                        Log.Message ("Found channel: ", chan);
                    } catch (ArgumentException ex) {
                        Log.Message ("Invalid channel in ", CHANNELS_CONF, ":", lineNum, ": ", ex.Message);
                    }
                } else if (parts.Length > 0) {
                    Log.Message ("Invalid channel in ", CHANNELS_CONF, ":", lineNum);
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
            int i = 0;
            foreach (Channel chan in channels) {
                mbsyncrc += "Channel chan" + i + "\nMaster \":" + chan.FromAccount.Accountname + "-imap:" + chan.FromPath + "\"\nSlave \":" + chan.ToAccount.Accountname + "-imap:" + chan.ToPath + "\"\nCreate Slave\nExpunge Both\nSync Pull\n\n";
                ++i;
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
        private string CHANNELS_CONF = "channels.conf";
        private string IMAPFILTER_LUA = "imapfilter.lua";
        private string MBSYNC_RC = "mbsyncrc";

        private string CONTENT_BIN_RUN1 { get { return "imapfilter -v -c " + fs.Runtime.RootDirectory + SystemInfo.PathSeparator + IMAPFILTER_LUA; } }

        private string CONTENT_BIN_RUN2 { get { return "mbsync -c " + fs.Runtime.RootDirectory + SystemInfo.PathSeparator + MBSYNC_RC + " -a"; } }
    }
}
