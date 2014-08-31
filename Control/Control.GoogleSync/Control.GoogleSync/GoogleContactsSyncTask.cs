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
        }

        protected override void InternalRun (string[] args)
        {
            foreach (GoogleAccount acc in GoogleAccount.List()) {
                Log.Message ("Google Account: ", acc);
                Log.Indent ++;
                ContactsAccess contacts = new ContactsAccess (account: acc);
                contacts.PrintAllContacts ();
                Log.Indent --;
            }
        }
    }
}

