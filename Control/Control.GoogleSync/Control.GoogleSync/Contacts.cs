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
    public class Contacts : Library
    {
        private RequestSettings settings;
        private GoogleAccount account;

        private static ConfigFile _syncConfig;

        private static ConfigFile syncConfig { get { return _syncConfig = _syncConfig ?? new Contacts ().fs.Config.OpenConfigFile ("sync.ini"); } }


        public static HashSet<string> IncludeNames {
            get {
                return syncConfig ["General", "IncludeNames", ""].SplitValues ().ToHashSet ();
            }
            set {
                syncConfig ["General", "IncludeNames", ""] = value.JoinValues ();
            }
        }

        public static HashSet<string> MasterAccountIds {
            get {
                return syncConfig ["General", "MasterAccounts", ""].SplitValues ().ToHashSet ();
            }
            set {
                syncConfig ["General", "MasterAccounts", ""] = value.JoinValues ();
            }
        }

        public Contacts (GoogleAccount account)
            : this ()
        {
            this.account = account;

            NetworkHelper.DisableCertificateChecks ();

            OAuth2Parameters parameters = account.GetOAuth2Parameters ();
            settings = new RequestSettings (GoogleApp.ApplicationName, parameters);
        }

        private Contacts ()
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

        private Contact[] _contactList;

        public Contact[] ContactList {
            get {
                if (_contactList == null) {
                    ContactsRequest cr = new ContactsRequest (settings);

                    ContactsQuery query = new ContactsQuery (ContactsQuery.CreateContactsUri ("default"));
                    query.NumberToRetrieve = 9999;
                    Feed<Contact> feed = cr.Get<Contact> (query);

                    _contactList = (from c in feed.Entries
                                                   where c.Name.FullName != null
                                                   orderby c.Name.FullName
                                                   select c).ToArray ();
                }
                return _contactList;
            }
        }

        public void PrintAllContacts ()
        {
            CatchErrors (() => {
                Log.Message (ContactList.ToStringTable (
                    c => c.IsIncludedInSynchronisation () ? (account.IsMasterAccount () ? LogColor.DarkYellow : LogColor.DarkCyan) : LogColor.Reset,
                    new[] { "Full Name", "E-Mail Address", "Role" },
                    c => c.Name.FullName,
                    c => c.Emails.PrintShort (),
                    c => c.IsIncludedInSynchronisation () ? (account.IsMasterAccount () ? "master" : "slave") : ""
                ));
            });
        }

        public void SyncTo (Contacts other)
        {
            Log.Message ("Synchronizing contacts: ", account, " => ", other.account);
            Log.Indent++;
            CatchErrors (() => {
                Contact[] masterContacts = (from c in ContactList
                                                        where c.IsIncludedInSynchronisation ()
                                                        select c).ToArray ();

                foreach (Contact masterContact in masterContacts) {
                    Log.Message ("Contact: ", masterContact.Print ());
                    Log.Indent++;

                    Contact slaveContact;
                    if (other.FindContact (checkFor: masterContact, result: out slaveContact)) {
                        Log.Message ("Update contact: ", slaveContact.Print ());
                    } else {
                        Log.Message ("Create contact.");
                    }

                    Log.Indent--;
                }
            });
            Log.Indent--;
        }

        public bool HasContact (Contact checkFor)
        {
            Contact dummy;
            return FindContact (checkFor: checkFor, result: out dummy);
        }

        public bool FindContact (Contact checkFor, out Contact result)
        {
            foreach (Contact contact in ContactList) {
                if (contact.Name.FullName == checkFor.Name.FullName) {
                    result = contact;
                    return true;
                }
            }
            foreach (Contact contact in ContactList) {
                foreach (EMail email1 in contact.Emails) {
                    foreach (EMail email2 in checkFor.Emails) {
                        if (email1.Address == email2.Address) {
                            result = contact;
                            return true;
                        }
                    }
                }
            }
            result = null;
            return false;
        }
    }
}

