using System;
using Control.Common.IO;
using Google.GData.Client;
using Google.Contacts;
using Google.GData.Extensions;
using Control.Common;

namespace Control.GoogleSync
{
    public class ContactsAccess : Library
    {
        private RequestSettings settings;
        private GoogleAccount account;

        public ContactsAccess (GoogleAccount account)
        {
            ConfigName = "Google";
            this.account = account;

            NetworkHelper.DisableCertificateChecks ();

            OAuth2Parameters parameters = account.GetOAuth2Parameters ();
            settings = new RequestSettings (GoogleApp.ApplicationName, parameters);
        }

        public void CatchErrors (Action todo)
        {
            try {
                todo ();
            } catch (GDataRequestException ex) {
                if (ex.InnerException.Message.Contains ("wrong scope")) {
                    account.Reauthenticate ();
                    todo ();
                } else {
                    Log.Error (ex);
                }
            }
        }

        public void PrintAllContacts ()
        {
            CatchErrors (() => {
                ContactsRequest cr = new ContactsRequest (settings);

                Feed<Contact> f = cr.GetContacts ();
                foreach (Contact entry in f.Entries) {
                    if (entry.Name != null) {
                        Name name = entry.Name;
                        Log.Debug (name.FullName);
                    }
                    Log.Indent ++;
                    foreach (EMail email in entry.Emails) {
                        Log.Debug (email.Address);
                    }
                    Log.Indent --;
                }
            });
        }
    }
}

