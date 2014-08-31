using System;
using Control.Common.Tasks;
using Control.Common.IO;
using System.Linq;

namespace Control.GoogleSync
{
    public class GoogleContactsTask : Task, MainTask
    {
        public GoogleContactsTask ()
        {
            Name = "GoogleContacts";
            Description = "fuck";
            Options = new string[] { "google-contacts", "g-contacts" };
            ConfigName = "Google";
            ParameterSyntax = "list | config | sync";
        }

        protected override void InternalRun (string[] args)
        {
            if (args.Length >= 1) {
                switch (args [0].ToLower ()) {
                case "config":
                    config ();
                    break;
                case "list":
                    list ();
                    break;
                case "sync":
                    sync ();
                    break;
                default:
                    error ();
                    break;
                }
            } else {
                error ();
            }
        }

        void list ()
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Message ("Google Account: ", acc);
                Log.Indent++;
                acc.Refresh ();
                ContactsAccess contacts = new ContactsAccess (account: acc);
                contacts.PrintAllContacts ();
                Log.Indent--;
            }
        }

        void config ()
        {
            printIncludedNames ();
            chooseAction ();
        }

        void printIncludedNames ()
        {
            Log.Message ("Contacts which contain the following name are synchronized:");
            Log.Indent++;
            if (ContactsAccess.IncludeNames.Any ()) {
                foreach (string name in ContactsAccess.IncludeNames) {
                    Log.Message (name);
                }
            } else {
                Log.Message ("None.");
            }
            Log.Indent--;
        }

        void chooseAction ()
        {
            Log.UserChoice ("What do you want to do?", new UserChoice (1, "Add an included name", addIncludedName), new UserChoice (2, "Remove an included name", addIncludedName), new UserChoice (3, "List all included names", addIncludedName));
        }

        void addIncludedName ()
        {

        }

        void sync ()
        {
            throw new NotImplementedException ();
        }

        void error ()
        {
            Log.Error ("One of the following options is required: " + ParameterSyntax);
        }
    }
}

