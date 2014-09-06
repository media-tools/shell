using System;
using Shell.Common.Tasks;
using Shell.Common.IO;
using System.Linq;
using System.Collections.Generic;
using Shell.Common.Util;

namespace Shell.GoogleSync
{
    public class GoogleContactsTask : ScriptTask, MainScriptTask
    {
        public GoogleContactsTask ()
        {
            Name = "GoogleContacts";
            Description = "fuck";
            Options = new string[] { "google-contacts", "g-contacts" };
            ConfigName = "Google";
            ParameterSyntax = "list | config | sync | clean";
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "config":
                    config ();
                    break;
                case "list":
                    listContacts ();
                    break;
                case "sync":
                    sync ();
                    break;
                case "clean":
                    clean ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
        }

        void listContacts ()
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Message ("Google Account: ", acc);
                Log.Indent++;
                acc.Refresh ();
                Contacts contacts = new Contacts (account: acc);
                contacts.PrintAllContacts ();
                Log.Indent--;
            }
        }

        void config ()
        {
            chooseAction ();
        }

        void chooseAction ()
        {
            bool keepRunning = true;
            while (keepRunning) {
                Log.UserChoice ("What do you want to do?",
                    new UserChoice (1, "List all contacts of all accounts", listContacts),
                    new UserChoice (2, "List all accounts", listAccounts),
                    new UserChoice (3, "Set an account as master", setMasterAccount),
                    new UserChoice (4, "Set an account as slave", setSlaveAccount),
                    new UserChoice (5, "List all included names", printIncludedNames),
                    new UserChoice (6, "Add an included name", addIncludedName),
                    new UserChoice (7, "Remove an included name", removeIncludedName),
                    new UserChoice ("q", "Exit", () => keepRunning = false)
                );
            }
        }

        void listAccounts ()
        {
            IEnumerable<GoogleAccount> accounts = GoogleAccount.List ();
            if (accounts.Any ()) {
                Log.Message (accounts.ToStringTable (
                    acc => acc.IsMasterAccount () ? LogColor.DarkYellow : LogColor.DarkCyan,
                    new[] { "Name", "E-Mail Address", "ID", "Role" },
                    acc => acc.DisplayName,
                    acc => acc.Emails,
                    acc => acc.Id,
                    acc => acc.IsMasterAccount () ? "master" : "slave"
                ));

                if (!accounts.Where (acc => acc.IsMasterAccount ()).Any ()) {
                    Log.Error ("There are no master accounts!");
                }
                if (!accounts.Where (acc => acc.IsSlaveAccount ()).Any ()) {
                    Log.Error ("There are no slave accounts!");
                }
            } else {
                Log.Message ("There are no accounts.");
            }
        }

        void setMasterAccount ()
        {
            changeAccount (question: "Which account do you want to set as master?", action: setMasterAccount);
            listAccounts ();
        }

        void setSlaveAccount ()
        {
            changeAccount (question: "Which account do you want to set as slave?", action: setSlaveAccount);
            listAccounts ();
        }

        void changeAccount (string question, Action<GoogleAccount> action)
        {
            IEnumerable<GoogleAccount> accounts = GoogleAccount.List ();
            if (accounts.Any ()) {
                Log.UserChoice ("Which account do you want to set as master?", choices: accounts.ToUserChoices (action));
            } else {
                Log.Message ("There are no accounts.");
            }
        }

        void setMasterAccount (GoogleAccount account)
        {
            Log.Message ("Set as master: ", account);
            HashSet<string> ids = Contacts.MasterAccountIds;
            ids.Add (account.Id);
            Contacts.MasterAccountIds = ids;
        }

        void setSlaveAccount (GoogleAccount account)
        {
            Log.Message ("Set as slave: ", account);
            HashSet<string> ids = Contacts.MasterAccountIds;
            ids.Remove (account.Id);
            Contacts.MasterAccountIds = ids;
        }

        void printIncludedNames ()
        {
            Log.Message ("Contacts whose name contains one the following strings are synchronized:");
            Log.Indent++;
            if (Contacts.IncludeNames.Any ()) {
                foreach (string name in Contacts.IncludeNames) {
                    Log.Message ("- ", LogColor.DarkCyan, name, LogColor.Reset);
                }
            } else {
                Log.Message ("None.");
            }
            Log.Indent--;
        }

        void addIncludedName ()
        {
            string name = Log.AskForString ("Which name do you want to add? ");
            HashSet<string> names = Contacts.IncludeNames;
            names.Add (name);
            Contacts.IncludeNames = names;
        }

        void removeIncludedName ()
        {
            string name = Log.AskForString ("Which name do you want to remove? ");
            HashSet<string> names = Contacts.IncludeNames;
            names.Remove (name);
            Contacts.IncludeNames = names;
        }

        void sync ()
        {
            IEnumerable<GoogleAccount> accounts = GoogleAccount.List ();
            accounts.ForEach (acc => acc.Refresh ());
            IEnumerable<GoogleAccount> masters = accounts.Where (acc => acc.IsMasterAccount ());
            IEnumerable<GoogleAccount> slaves = accounts.Where (acc => acc.IsSlaveAccount ());
            if (!accounts.Any ()) {
                Log.Error ("There are no accounts.");
            } else if (!masters.Any ()) {
                Log.Error ("There are no master accounts!");
            } else if (!slaves.Any ()) {
                Log.Error ("There are no slave accounts!");
            } else {
                foreach (GoogleAccount master in masters) {
                    foreach (GoogleAccount slave in slaves) {
                        Contacts masterContacts = new Contacts (account: master);
                        Contacts slaveContacts = new Contacts (account: slave);
                        masterContacts.SyncTo (otherContacts: slaveContacts);
                    }
                }
            }
        }

        void clean ()
        {
            IEnumerable<GoogleAccount> accounts = GoogleAccount.List ();
            accounts.ForEach (acc => acc.Refresh ());
            IEnumerable<GoogleAccount> masters = accounts.Where (acc => acc.IsMasterAccount ());
            IEnumerable<GoogleAccount> slaves = accounts.Where (acc => acc.IsSlaveAccount ());
            if (!accounts.Any ()) {
                Log.Error ("There are no accounts.");
            } else if (!masters.Any ()) {
                Log.Error ("There are no master accounts!");
            } else if (!slaves.Any ()) {
                Log.Error ("There are no slave accounts!");
            } else {
                foreach (GoogleAccount account in slaves) {
                    Contacts contacts = new Contacts (account: account);
                    contacts.CleanContacts ();
                }
                foreach (GoogleAccount account in masters) {
                    Contacts contacts = new Contacts (account: account);
                    contacts.CleanContacts ();
                }
            }
        }

        void error ()
        {
            Log.Error ("One of the following options is required: " + ParameterSyntax);
        }
    }
}

