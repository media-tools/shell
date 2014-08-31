using System;
using Control.Common.Tasks;
using Control.Common.IO;

namespace Control.GoogleSync
{
    public class GoogleContactsSyncTask : Task, MainTask
    {
        public GoogleContactsSyncTask ()
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
                Log.Indent ++;
                acc.Refresh ();
                ContactsAccess contacts = new ContactsAccess (account: acc);
                contacts.PrintAllContacts ();
                Log.Indent --;
            }
        }

        void config ()
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

