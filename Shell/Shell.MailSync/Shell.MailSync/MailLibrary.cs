using System;
using Shell.Common;
using System.Collections.Generic;
using Shell.Common.Tasks;
using Shell.Common.IO;
using Shell.Common.Util;
using System.Text.RegularExpressions;
using System.Linq;

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
                        Log.Info ("Found account: ", acc);
                    } else {
                        Log.Info ("Invalid google account in ", ACCOUNTS_CONF, ":", lineNum);
                    }
                } else if (parts.Length >= 1 && parts [0] == "custom") {
                    if (parts.Length == 5) {
                        Account acc = new Account (accountname: parts [1], username: parts [2], password: parts [3], hostname: parts [4]);
                        accounts.Add (acc);
                        Log.Info ("Found account: ", acc);
                    } else {
                        Log.Info ("Invalid custom account in ", ACCOUNTS_CONF, ":", lineNum);
                    }
                } else if (parts.Length > 0) {
                    Log.Info ("Invalid account in ", ACCOUNTS_CONF, ":", lineNum);
                }
                ++lineNum;
            }
        }

        private Dictionary<string,string> ParseParameters (string parameters_str)
        {
            string pattern = "(?<key>[a-z-]+)(=\")(?<value>.*?)(\")";
            MatchCollection matches = Regex.Matches (parameters_str, pattern);
            Dictionary<string,string> parameters = new Dictionary<string, string> ();
            foreach (Match match in matches) {
                parameters [match.Groups ["key"].Value.ToLower ()] = match.Groups ["value"].Value;
            }
            return parameters;
        }

        private void readConfigChannels ()
        {
            if (!fs.Config.FileExists (path: CHANNELS_CONF)) {
                fs.Config.WriteAllText (path: CHANNELS_CONF, contents: ""
                + "copy ACCOUT_NAME_1:INBOX ACCOUT_NAME_2:Archiv\n"
                + "copy ACCOUT_NAME_1:PATH -> ACCOUT_NAME_3:PATH\n"
                + "move ACCOUT_NAME_2:* -> ACCOUT_NAME_1:PATH\n");
            }

            string[] lines = fs.Config.ReadAllLines (path: CHANNELS_CONF);
            int lineNum = 1;
            List<Dictionary<string,string>> globalParameters = new List<Dictionary<string, string>> ();

            foreach (string line in from line in lines select line.Trim ('\t', ' ')) {
                if (line.StartsWith ("//") || line.StartsWith ("#")) {
                    continue;
                }

                try {
                    string[] parts = line.Split (separator: new [] { " ", "\t" }, count: 2, options: StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2) {
                        string operation_str = parts [0];
                        string parameters_str = parts [1];

                        if (operation_str == "set" && parameters_str.EndsWith ("{")) {
                            Dictionary<string,string> parameters = ParseParameters (parameters_str);
                            globalParameters.Add (parameters);
                        } else {
                            ChannelOperation op = Channel.ParseOperation (operation_str);
                            Dictionary<string,string> parameters = ParseParameters (parameters_str);
                            parameters = globalParameters.SelectMany (d => d).Union (parameters).ToDictionary (x => x.Key, x => x.Value);

                            if (op == ChannelOperation.DELETE) {
                                if (parameters.ContainsKey ("from") && parameters.ContainsKey ("filter")) {
                                    parameters ["to"] = parameters ["from"].Split (':') [0] + ":" + MailKit.SpecialFolder.Trash;
                                } else {
                                    throw new ArgumentException ("delete needs a 'from' and a 'filter' parameter");
                                }
                            }

                            if (parameters.ContainsKey ("from") && parameters.ContainsKey ("to")) {
                                string from = parameters ["from"];
                                string to = parameters ["to"];
                                parameters.Remove ("from");
                                parameters.Remove ("to");
                                Channel chan = new Channel (accounts: accounts, from: from, to: to, op: op, parameters: parameters);
                                channels.Add (chan);
                                Log.Info ("Found channel: ", chan);
                            } else {
                                throw new ArgumentException ("one of the following parameters is missing: 'from' or 'to' ");
                            }
                        }
                    } else if (line.EndsWith ("}")) {
                        if (globalParameters.Count > 0) {
                            globalParameters.RemoveAt (globalParameters.Count - 1);
                        } else {
                            throw new ArgumentException ("there is no block left to close: " + line);
                        }
                    }
                } catch (ArgumentException ex) {
                    Log.Info ("Invalid channel in ", CHANNELS_CONF, ":", lineNum, ": ", ex.Message);
                }

                ++lineNum;
            }
        }

        private string ACCOUNTS_CONF = "accounts.conf";
        private string CHANNELS_CONF = "channels.conf";
    }
}
