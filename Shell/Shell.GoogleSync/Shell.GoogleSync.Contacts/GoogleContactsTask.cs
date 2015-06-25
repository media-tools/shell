using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Options;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.GoogleSync.Core;
using Shell.Namespaces;

namespace Shell.GoogleSync.Contacts
{
    public class GoogleContactsTask : MonoOptionsScriptTask, MainScriptTask
    {
        public GoogleContactsTask ()
        {
            Name = "GoogleContacts";
            Options = new [] { "google-contacts" };
            ConfigName = NamespaceGoogle.CONFIG_NAME;

            Description = new [] {
                "List the google contacts of all users",
                "Configure the google contacts",
                "Synchronize the google contacts",
                "Clean the google contacts of all users"
            };
            Parameters = new [] {
                "list",
                "config",
                "sync",
                "clean"
            };
            Methods = new Action[] {
                () => listContacts (),
                () => config (),
                () => sync (),
                () => clean (),
            };
        }

        protected override void SetupOptions (ref OptionSet optionSet)
        {
        }

        void listContacts ()
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Info ("Google Account: ", acc);
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
                Log.Info (accounts.ToStringTable (
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
                Log.Info ("There are no accounts.");
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
                Log.Info ("There are no accounts.");
            }
        }

        void setMasterAccount (GoogleAccount account)
        {
            Log.Info ("Set as master: ", account);
            HashSet<string> ids = Contacts.MasterAccountIds;
            ids.Add (account.Id);
            Contacts.MasterAccountIds = ids;
        }

        void setSlaveAccount (GoogleAccount account)
        {
            Log.Info ("Set as slave: ", account);
            HashSet<string> ids = Contacts.MasterAccountIds;
            ids.Remove (account.Id);
            Contacts.MasterAccountIds = ids;
        }

        void printIncludedNames ()
        {
            Log.Info ("Contacts whose name contains one the following strings are synchronized:");
            Log.Indent++;
            if (Contacts.IncludeNames.Any ()) {
                foreach (string name in Contacts.IncludeNames) {
                    Log.Info ("- ", LogColor.DarkCyan, name, LogColor.Reset);
                }
            } else {
                Log.Info ("None.");
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
                        slaveContacts.Deduplicate ();
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
    }
}

