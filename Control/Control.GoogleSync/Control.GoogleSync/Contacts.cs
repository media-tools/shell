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
                return syncConfig ["General", "IncludeNames", ""].SplitValues ().Where (v => v.Length > 1).ToHashSet ();
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
                    Log.Error ("GDataRequestException: ", ex.ResponseString);
                    // Log.Error (ex);
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

        public void SyncTo (Contacts otherContacts)
        {
            Log.Message ("Synchronizing contacts: ", account, " => ", otherContacts.account);
            Log.Indent++;
            CatchErrors (() => {
                Contact[] masterContacts = (from c in ContactList
                                                        where c.IsIncludedInSynchronisation ()
                                                        select c).ToArray ();

                foreach (Contact masterContact in masterContacts) {
                    Log.Message ("Contact: ", masterContact);
                    Log.Indent++;

                    Contact slaveContact;
                    if (otherContacts.FindContact (checkFor: masterContact, result: out slaveContact)) {
                        Log.Message ("Update contact: ", slaveContact);
                        otherContacts.UpdateContact (slave: slaveContact, master: masterContact);
                    } else {
                        Log.Message ("Create contact.");
                        otherContacts.CreateContact (template: masterContact);
                    }

                    Log.Indent--;
                }
            });
            Log.Indent--;
        }

        public void UpdateContact (Contact slave, Contact master)
        {
            CatchErrors (() => {
                MergeContact (slave: slave, master: master);

                ContactsRequest cr = new ContactsRequest (settings);
                cr.Update (slave);
            });
        }

        public void CreateContact (Contact template)
        {
            CatchErrors (() => {
                Contact newContact = new Contact ();

                MergeContact (slave: newContact, master: template);

                Uri feedUri = new Uri (ContactsQuery.CreateContactsUri ("default"));
                ContactsRequest cr = new ContactsRequest (settings);
                Contact createdEntry = cr.Insert (address: feedUri, entry: newContact);
                Log.Message ("Contact's ID: " + createdEntry.Id);
            });
        }

        void MergeContact (Contact slave, Contact master)
        {
            slave.Name = master.Name.Format ();
            slave.ContactEntry.Birthday = master.ContactEntry.Birthday;
            slave.Organizations.Clear ();

            Log.Debug ("Birthday:", slave.ContactEntry.Birthday);

            GDataExtensions.Merge (slave.Emails, master.Emails, mail => mail.Address);
            slave.Emails.ForEach (mail => mail.Primary = slave.Emails.Count > 1 && mail.Address == slave.Emails.PrimaryAddress ());
            GDataExtensions.Merge (slave.Organizations, master.Organizations, org => org.JobDescription + org.Title + org.Department + org.Name + org.Location);
            GDataExtensions.Merge (slave.Languages, master.Languages, l => l.Value);
            GDataExtensions.Merge (slave.IMs, master.IMs, l => l.Value);
            GDataExtensions.Merge (slave.Phonenumbers, master.Phonenumbers, l => l.Value, GDataExtensions.UniqueFormat);
            slave.Phonenumbers.Remove (slave.Phonenumbers.Where (n => n.Value == "+4915234218133").FirstOrDefault ());
            GDataExtensions.Merge (slave.PostalAddresses, master.PostalAddresses, a => a.City);

            Log.Debug ("Emails:", string.Join (", ", slave.Emails.Select (e => e.Address)));
            Log.Debug ("Organizations:", string.Join (", ", slave.Organizations.Select (org => org.JobDescription + org.Title + org.Department + org.Name + org.Location)));
            Log.Debug ("Languages:", string.Join (", ", slave.Languages.Select (l => l.Value)));
            Log.Debug ("IMs:", string.Join (", ", slave.IMs.Select (i => i.Value)));
            Log.Debug ("Phonenumbers:", string.Join (", ", slave.Phonenumbers.Select (p => p.Value + " (" + (p.Rel != null ? p.Rel : p.Label) + ")")));
            Log.Debug ("PostalAddresses:", string.Join ("; ", slave.PostalAddresses.Select (a => a.Format ())));
        }

        public bool HasContact (Contact checkFor)
        {
            Contact dummy;
            return FindContact (checkFor: checkFor, result: out dummy);
        }

        public bool FindContact (Contact checkFor, out Contact result)
        {
            foreach (Contact contact in ContactList) {
                if (contact.Name.FullName.ToLower ().RemoveNonAlphanumeric () == checkFor.Name.FullName.ToLower ().RemoveNonAlphanumeric ()) {
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

        public void CleanContacts ()
        {
            Log.Message ("Cleaning contacts: ", account);
            Log.Indent++;
            CatchErrors (() => {
                foreach (Contact contact in ContactList) {
                    Log.Message ("Contact: ", contact);
                    Log.Indent++;

                    CleanContact (contact: contact);

                    Log.Indent--;
                }
            });
            Log.Indent--;
        }

        void CleanContact (Contact contact)
        {
            contact.Name = contact.Name.Format ();
            contact.Organizations.Clear ();

            Log.Debug ("Family Name: ", contact.Name.FamilyName.FormatName ());
            Log.Debug ("Given Name: ", contact.Name.GivenName.FormatName ());
            Log.Debug ("Birthday:", contact.ContactEntry.Birthday);

            GDataExtensions.Merge (contact.Emails, contact.Emails, mail => mail.Address);
            contact.Emails.ForEach (mail => mail.Primary = contact.Emails.Count > 1 && mail.Address == contact.Emails.PrimaryAddress ());
            GDataExtensions.Merge (contact.Organizations, contact.Organizations, org => org.JobDescription + org.Title + org.Department + org.Name + org.Location);
            GDataExtensions.Merge (contact.Languages, contact.Languages, l => l.Value);
            GDataExtensions.Merge (contact.IMs, contact.IMs, l => l.Value);
            GDataExtensions.Merge (contact.Phonenumbers, contact.Phonenumbers, l => l.Value, GDataExtensions.UniqueFormat);
            GDataExtensions.Merge (contact.PostalAddresses, contact.PostalAddresses, a => a.City);

            Log.Debug ("Emails:", string.Join (", ", contact.Emails.Select (e => e.Address)));
            Log.Debug ("Organizations:", string.Join (", ", contact.Organizations.Select (org => org.JobDescription + org.Title + org.Department + org.Name + org.Location)));
            Log.Debug ("Languages:", string.Join (", ", contact.Languages.Select (l => l.Value)));
            Log.Debug ("IMs:", string.Join (", ", contact.IMs.Select (i => i.Value)));
            Log.Debug ("Phonenumbers:", string.Join (", ", contact.Phonenumbers.Select (p => p.Value + " (" + (p.Rel != null ? p.Rel : p.Label) + ")")));
            Log.Debug ("PostalAddresses:", string.Join ("; ", contact.PostalAddresses.Select (a => a.Format ())));

            CatchErrors (() => {
                ContactsRequest cr = new ContactsRequest (settings);
                cr.Update (contact);
            });
        }
    }
}

