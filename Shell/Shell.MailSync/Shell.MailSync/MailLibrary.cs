using System;
using Shell.Common;
using System.Collections.Generic;
using Shell.Common.Tasks;
using Shell.Common.IO;
using Shell.Common.Util;

namespace Shell.MailSync
{
    public class MailLibrary
    {
        private static List<Account> accounts = new List<Account> ();
        private static List<Channel> channels = new List<Channel> ();
		private FileSystems fs;

		public IEnumerable<Account> Accounts { get { return accounts; } }

		public IEnumerable<Channel> Channels { get { return channels; } }

        public MailLibrary (FileSystems filesystems)
        {
            fs = filesystems;
        }

        public void UpdateConfigs ()
        {
            readConfigs ();
            writeConfigs ();
        }

        private void readConfigs ()
        {
            readConfigAccounts ();
            readConfigChannels ();
        }

        private void readConfigAccounts ()
        {
            if (!fs.Config.FileExists (path: ACCOUNTS_CONF)) {
                fs.Config.WriteAllText (path: ACCOUNTS_CONF, contents: "google ACCOUT_NAME_1 USER_NAME PASSWORD\n" + "custom ACCOUT_NAME_2 USER_NAME PASSWORD IMAP_HOST\n");
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
                } else if (parts.Length >= 1 && parts [0] == "custom") {
                    if (parts.Length == 5) {
                        Account acc = new Account (accountname: parts [1], username: parts [2], password: parts [3], hostname: parts [4]);
                        accounts.Add (acc);
                        Log.Message ("Found account: ", acc);
                    } else {
                        Log.Message ("Invalid custom account in ", ACCOUNTS_CONF, ":", lineNum);
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
                fs.Config.WriteAllText (path: CHANNELS_CONF, contents: "ACCOUT_NAME_1:INBOX -> ACCOUT_NAME_2:Archiv\n"
                    + "ACCOUT_NAME_1:PATH -> ACCOUT_NAME_3:PATH\n"
                    + "ACCOUT_NAME_1:PATH -> ACCOUT_NAME_2:PATH\n");
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

            string imapfilter_lua = "";
            imapfilter_lua += IMAPFILTER_LUA_BEGIN;
            foreach (Account acc in accounts) {
                imapfilter_lua += "    local " + acc.Accountname + " = IMAP {\n" +
                    "server = '" + acc.Hostname + "',\n" +
                    "username = '" + acc.Username + "',\n" +
                    "password = '" + acc.Password + "',\n" +
                    "ssl = 'tls1',\n" +
                    "}\n";
                imapfilter_lua += "    move(" + acc.Accountname + ", \"INBOX\", \"Archiv\", is_expired(30))\n";
            }
            imapfilter_lua += IMAPFILTER_LUA_END;
            fs.Runtime.WriteAllText (path: IMAPFILTER_LUA, contents: imapfilter_lua);
        }

        public void RunImapFilter ()
        {
            fs.Runtime.WriteAllText (path: "run1.sh", contents: CONTENT_BIN_RUN_IMAPFILTER);
            fs.Runtime.ExecuteScript (path: "run1.sh");
        }

        public void RunMbsync ()
        {
            fs.Runtime.WriteAllText (path: "run2.sh", contents: CONTENT_BIN_RUN_MBSYNC);
            fs.Runtime.ExecuteScript (path: "run2.sh");
        }

        private string ACCOUNTS_CONF = "accounts.conf";
        private string CHANNELS_CONF = "channels.conf";
        private string IMAPFILTER_LUA = "imapfilter.lua";
        private string MBSYNC_RC = "mbsyncrc";

        private string CONTENT_BIN_RUN_IMAPFILTER {
            get {
                return "function faketty { script -qfc \"$(printf \"'%s' \" \"$@\")\"; }\n" +
                    "yes p | faketty imapfilter -v -c " + fs.Runtime.RootDirectory + SystemInfo.PathSeparator + IMAPFILTER_LUA;
            }
        }

        private string CONTENT_BIN_RUN_MBSYNC {
            get {
                return "mbsync -c " + fs.Runtime.RootDirectory + SystemInfo.PathSeparator + MBSYNC_RC + " -a";
            }
        }

        private string IMAPFILTER_LUA_BEGIN = "options.certificates = true\n" +
            "function main()\n";
        private string IMAPFILTER_LUA_END = "end\n" +
            "\n" +
            "-- This helper function lets one copy messages\n" +
            "-- between two folders by name, rather than value,\n" +
            "-- and takes a function callback (f) to act as a \"predicate\".\n" +
            "function copy(imap, from_name, to_name, f)\n" +
            "local from = imap[from_name]\n" +
            "local to   = imap[to_name]\n" +
            "from:copy_messages(to, f(from))\n" +
            "end\n" +
            " \n" +
            "-- This is almost identical to copy(), except it moves messages\n" +
            "-- instead.\n" +
            "function move(imap, from_name, to_name, f)\n" +
            "    local from = imap[from_name]\n" +
            "    local to   = imap[to_name]\n" +
            "    from:move_messages(to, f(from))\n" +
            "end\n" +
            "\n" +
            "function move2(imap1, from_name, imap2, to_name, f)\n" +
            "    local from = imap1[from_name]\n" +
            "    local to   = imap2[to_name]\n" +
            "    from:move_messages(to, f(from))\n" +
            "end\n" +
            " \n" +
            "-- Here, I use the verb \"archive\", as I use gmail's imap.\n" +
            "-- Deleting mail from a label (or inbox) == archiving it.\n" +
            "function archive(imap, from_name, f)\n" +
            "    local from = imap[from_name]\n" +
            "    from:delete_messages( f(from) )\n" +
            "end\n" +
            " \n" +
            "-- Trashing mail is the same as moving it to the Trash folder.\n" +
            "function trash(imap, name, f)\n" +
            "    move(imap, name, '[Gmail]/Trash', f)\n" +
            "end\n" +
            " \n" +
            "-- This is a shortcut for removing mail regardless of which label(es) it is in.\n" +
            "function trash_all(imap, f)\n" +
            "    trash(imap, '[Gmail]/All Mail', f)\n" +
            "end\n" +
            " \n" +
            " \n" +
            "-- The following are predicate function builders,\n" +
            "-- which is to say they are functions which return functions which return a set of messages to act on.\n" +
            "-- There is nothing special about '_', it is just the 'from' folder object (c.f. move() and copy()).\n" +
            " \n" +
            "-- An ignored message is something older than age and unseen (unread).\n" +
            "function is_ignored(age)\n" +
            "    return function (_) return _:is_older(age) * _:is_unseen() end\n" +
            "end\n" +
            " \n" +
            "-- This matches all messages older than age and unflagged (unstarred in gmail terms)\n" +
            "function is_older(age) \n" +
            "    return function (_) return _:is_older(age) * _:is_unflagged() end \n" +
            "end\n" +
            " \n" +
            "-- Same, except ignores unseen messages. Used on the inbox, slug, and billing folders/labels.\n" +
            "function is_expired(age)\n" +
            "    return function (_) return _:is_older(age) * _:is_seen() * _:is_unflagged() end\n" +
            "------    return function (_) return _:is_newer(1) end\n" +
            "end\n" +
            " \n" +
            "-- This matches all messages whose subject contains the given text.\n" +
            "function has_subject_and_age(subj, age)\n" +
            "    return function (_) return _:contain_subject(subj) * _:is_older(age) * _:is_seen() * _:is_unflagged() end\n" +
            "end\n" +
            "\n" +
            "-- This matches all messages whose subject contains the given text.\n" +
            "function has_subject(subj)\n" +
            "    return function (_) return _:contain_subject(subj) end\n" +
            "end\n" +
            "\n" +
            " \n" +
            "main() -- and now we begin executing the meat of the script.\n";
    }
}
