using System;
using System.Collections.Generic;
using System.Linq;
using Control.Common.IO;
using Control.Common.Util;
using Google.Contacts;
using Google.GData.Client;
using Google.GData.Contacts;
using Google.GData.Extensions;

namespace Control.GoogleSync
{
    public class ContactsAccess : Library
    {
        private RequestSettings settings;
        private GoogleAccount account;

        private static ConfigFile _syncConfig;

        private static ConfigFile syncConfig { get { return _syncConfig = _syncConfig ?? new ContactsAccess ().fs.Config.OpenConfigFile ("sync.ini"); } }


        public static HashSet<string> IncludeNames {
            get {
                return syncConfig ["General", "IncludeNames", ""].SplitValues ().ToHashSet ();
            }
            set {
                syncConfig ["General", "IncludeNames", ""] = value.JoinValues ();
            }
        }

        public ContactsAccess (GoogleAccount account)
            : this ()
        {
            this.account = account;

            NetworkHelper.DisableCertificateChecks ();

            OAuth2Parameters parameters = account.GetOAuth2Parameters ();
            settings = new RequestSettings (GoogleApp.ApplicationName, parameters);
        }

        private ContactsAccess ()
        {
            ConfigName = "Google";
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

                ContactsQuery query = new ContactsQuery (ContactsQuery.CreateContactsUri ("default"));
                query.NumberToRetrieve = 9999;
                Feed<Contact> feed = cr.Get<Contact> (query);

                IEnumerable<Contact> contacts = from c in feed.Entries
                                                            where c.Name.FullName != null
                                                            orderby c.Name.FullName
                                                            select c;
                Log.Message (contacts.ToStringTable (new[] { "Full Name", "Emails" },
                    c => c.Name.FullName,
                    c => c.Emails.PrintShort ()
                ));
            });
        }
    }
}

