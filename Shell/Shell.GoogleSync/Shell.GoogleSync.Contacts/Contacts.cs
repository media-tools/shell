using System;
using System.Collections.Generic;
using System.Linq;
using Google.Contacts;
using Google.GData.Client;
using Google.GData.Contacts;
using Google.GData.Extensions;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.GoogleSync.Core;
using System.IO;

namespace Shell.GoogleSync.Contacts
{
    public class Contacts : GDataLibrary
    {
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
            : base (account)
        {
            LoadGroups ();
        }

        protected override void UpdateAuth ()
        {
        }

        private Contacts ()
            : base ()
        {
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

        private Dictionary<string,string> Groups = new Dictionary<string,string> ();

        private void LoadGroups ()
        {
            ContactsRequest cr = new ContactsRequest (settings);
            Feed<Group> fg = cr.GetGroups ();
            Groups.Clear ();
            foreach (Group group in fg.Entries) {
                Groups [group.Title] = group.Id;
            }
            Log.Message ("Groups: ", account);
            Log.Indent++;
            Log.Message (Groups.ToStringTable (
                c => LogColor.Reset,
                new[] { "Title", "ID" },
                c => c.Key,
                c => c.Value
            ));
            Log.Indent--;
        }

        public void PrintAllContacts ()
        {
            CatchErrors (() => {
                // Analysis disable once ConvertToLambdaExpression
                Log.Message (ContactList.ToStringTable (
                    c => c.IsIncludedInSynchronisation () ? (account.IsMasterAccount () ? LogColor.DarkYellow : LogColor.DarkCyan) : LogColor.Reset,
                    new[] { "Full Name", "E-Mail Address", "Phone", "Role" },
                    c => c.Name.FullName,
                    c => c.Emails.PrintShort (),
                    c => string.Join (", ", c.Phonenumbers.Select (p => p.Value + " (" + (p.Rel != null ? p.Rel.Split ('#').Last () : p.Label) + ")")),
                    c => c.IsIncludedInSynchronisation () ? (account.IsMasterAccount () ? "master" : "slave") : ""
                ));
            });
        }

        public void Deduplicate ()
        {
            Log.Message ("Deduplicate contacts: ", account);
            Log.Indent++;
            CatchErrors (() => {
                HashSet<string> names = new HashSet<string> ();
                foreach (Contact masterContact in ContactList.ToArray()) {
                    Log.Message ("Contact: ", masterContact);
                    Log.Indent++;

                    if (names.Contains (masterContact.Name.FullName)) {
                        Log.Message ("delete!");
                        DeleteContact (masterContact);
                    } else {
                        names.Add (masterContact.Name.FullName);
                    }

                    Log.Indent--;
                }
            });
            Log.Indent--;
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

                    Contact[] slaveContacts = otherContacts.FindContact (checkFor: masterContact).ToArray ();
                    if (slaveContacts.Length > 0) {
                        foreach (Contact slaveContact in slaveContacts) {
                            Log.Message ("Update contact: ", slaveContact);
                            otherContacts.UpdateContact (slave: slaveContact, master: masterContact, masterList: this);
                        }
                    } else {
                        Log.Message ("Create contact.");
                        otherContacts.CreateContact (template: masterContact, templateList: this);
                    }

                    Log.Indent--;
                }
            });
            Log.Indent--;
        }

        public void UpdateContact (Contact slave, Contact master, Contacts masterList)
        {
            CatchErrors (() => {
                MergeContact (slave: slave, master: master, masterList: masterList);

                ContactsRequest cr = new ContactsRequest (settings);
                cr.Update (slave);

                // TODO: hier hatte ich aufeghoert
                // MergePhoto (slave: slave, master: master, masterList: masterList);
            });
        }

        public void CreateContact (Contact template, Contacts templateList)
        {
            CatchErrors (() => {
                Contact newContact = new Contact ();

                MergeContact (slave: newContact, master: template, masterList: templateList);

                Uri feedUri = new Uri (ContactsQuery.CreateContactsUri ("default"));
                ContactsRequest cr = new ContactsRequest (settings);
                Contact createdEntry = cr.Insert (address: feedUri, entry: newContact);
                Log.Message ("Contact's ID: " + createdEntry.Id);
            });
        }


        void MergeContact (Contact slave, Contact master, Contacts masterList)
        {
            slave.Name = master.Name.Format ();
            //if (slave.Name.FamilyName != null && slave.Name.FamilyName.EndsWith ("berg")) {
            //    slave.Name.FamilyName = "RuXXXX";
            //    slave.Name.FullName = slave.Name.GivenName + " " + slave.Name.FamilyName;
            //}
            Log.Debug ("famnam: ", slave.Name.FamilyName);
            slave.ContactEntry.Birthday = master.ContactEntry.Birthday;
            slave.Organizations.Clear ();
            string emails = string.Join (", ", slave.Emails.Select (e => e.Address)).ToLower ();
            if (emails.Contains ("tobias") || emails.Contains ("thri") || emails.Contains ("isar") || emails.Contains ("tina")) {
                slave.Phonenumbers.Clear ();
                slave.Emails.Clear ();
            }
            slave.PostalAddresses.Clear ();

            Log.Debug ("Birthday:", slave.ContactEntry.Birthday);

            GDataContactExtensions.Merge (slave.Emails, master.Emails, mail => mail.Address);
            slave.Emails.ForEach (mail => mail.Primary = slave.Emails.Count > 1 && mail.Address == slave.Emails.PrimaryAddress ());
            GDataContactExtensions.Merge (slave.Organizations, master.Organizations, org => org.JobDescription + org.Title + org.Department + org.Name + org.Location);
            GDataContactExtensions.Merge (slave.Languages, master.Languages, l => l.Value);
            GDataContactExtensions.Merge (slave.IMs, master.IMs, l => l.Value);
            GDataContactExtensions.Merge (slave.Phonenumbers, master.Phonenumbers, l => l.Value, GDataContactExtensions.UniqueFormat);
            slave.Phonenumbers.Remove (slave.Phonenumbers.Where (n => n.Value == "+4915234218133").FirstOrDefault ());
            GDataContactExtensions.Merge (slave.PostalAddresses, master.PostalAddresses, a => a.City);


            string[] groupsToAdd = new [] { "System Group: My Contacts" }; //, "System Group: Family" };
            foreach (string groupToAdd in groupsToAdd) {
                if (Groups.ContainsKey (groupToAdd)) {
                    if (!slave.GroupMembership.Any (gm => gm.HRef == Groups [groupToAdd])) {
                        slave.GroupMembership.Add (new GroupMembership { HRef = Groups [groupToAdd] });
                    }
                }
            }
            

            GDataContactExtensions.Merge (slave.ContactEntry.Relations, master.ContactEntry.Relations, r => r.Value);
            slave.ContactEntry.Websites.Clear ();
            GDataContactExtensions.Merge (slave.ContactEntry.Websites, master.ContactEntry.Websites, w => w.Href);
            slave.ContactEntry.Nickname = master.ContactEntry.Nickname;
            slave.ContactEntry.ShortName = master.ContactEntry.ShortName;

            GDataContactExtensions.Merge (slave.ContactEntry.UserDefinedFields, master.ContactEntry.UserDefinedFields, ud => ud.Key + ud.Value);

            Log.Debug ("Emails:", string.Join (", ", slave.Emails.Select (e => e.Address)));
            Log.Debug ("Organizations:", string.Join (", ", slave.Organizations.Select (org => org.JobDescription + org.Title + org.Department + org.Name + org.Location)));
            Log.Debug ("Languages:", string.Join (", ", slave.Languages.Select (l => l.Value)));
            Log.Debug ("IMs:", string.Join (", ", slave.IMs.Select (i => i.Value)));
            Log.Debug ("Phonenumbers:", string.Join (", ", slave.Phonenumbers.Select (p => p.Value + " (" + (p.Rel != null ? p.Rel : p.Label) + ")")));
            Log.Debug ("PostalAddresses:", string.Join ("; ", slave.PostalAddresses.Select (a => a.Format ())));
            Log.Debug ("Relations:", string.Join ("; ", slave.ContactEntry.Relations.Select (r => r.Format ())));
            Log.Debug ("GroupMembership:", string.Join (", ", slave.GroupMembership.Select (gm => Groups.Where (e => e.Value == gm.HRef).Select (e => e.Key).First ())));
        }


        void MergePhoto (Contact slave, Contact master, Contacts masterList)
        {
            if (masterList != null) {
                byte[] masterPhoto = masterList.DownloadPhoto (master);
                byte[] slavePhoto = DownloadPhoto (slave);
                Log.Debug ("master photo: ", (masterPhoto != null) ? masterPhoto.Length : -1);
                Log.Debug ("slave photo: ", (slavePhoto != null) ? slavePhoto.Length : -1);
                if (masterPhoto != null) {
                    Log.Message ("Copy master photo to slave.");
                    UpdateContactPhoto (slave, masterPhoto);
                }
                if (masterPhoto == null && slavePhoto != null) {
                    Log.Message ("Reverse copy slave photo to master.");
                    masterList.UpdateContactPhoto (master, slavePhoto);
                }
            }
        }

        public byte[] DownloadPhoto (Contact contact)
        {
            try {
                ContactsRequest cr = new ContactsRequest (settings);
                Stream photoStream = cr.Service.Query (contact.ContactEntry.PhotoUri);
                if (photoStream != null) {
                    using (var memoryStream = new MemoryStream ()) {
                        photoStream.CopyTo (memoryStream);
                        return memoryStream.ToArray ();
                    }
                } else {
                    return null;
                }
            } catch (GDataRequestException) {
                return null;
            }
        }

        public void UpdateContactPhoto (Contact contact, byte[] photo)
        {
            ContactsRequest cr = new ContactsRequest (settings);
            using (var photoStream = new MemoryStream (photo)) {
                try {
                    FileStream fs = File.Create ("/tmp/test.jpg");
                    fs.Write (photo, 0, photo.Length);
                    fs.Close ();
                    //cr.Service.Update (contact.PhotoUri, photoStream, "image/jpeg", "");
                    Stream res = cr.Service.StreamSend (contact.PhotoUri, photoStream, GDataRequestType.Update, "image/jpeg", null, "*");
                    return;
                    GDataReturnStream r = res as GDataReturnStream;
                    if (r != null) {
                        contact.PhotoEtag = r.Etag;
                    }
                    res.Close ();
                    //cr.SetPhoto (contact, photoStream);
                } catch (GDataVersionConflictException ex) {
                    // Etags mismatch: handle the exception.
                    Log.Error (ex);
                } catch (GDataRequestException ex) {
                    // Etags mismatch: handle the exception.
                    Log.Error (ex.ResponseString);
                }
            }
            //Environment.Exit (0);
        }

        public bool HasContact (Contact checkFor)
        {
            return FindContact (checkFor: checkFor).Any ();
        }

        public IEnumerable<Contact> FindContact (Contact checkFor)
        {
            foreach (Contact contact in ContactList) {
                if (contact.Name.FullName.ToLower ().RemoveNonAlphanumeric () == checkFor.Name.FullName.ToLower ().RemoveNonAlphanumeric ()) {
                    yield return contact;
                }
            }
            foreach (Contact contact in ContactList) {
                foreach (EMail email1 in contact.Emails) {
                    foreach (EMail email2 in checkFor.Emails) {
                        if (email1.Address == email2.Address) {
                            yield return contact;
                        }
                    }
                }
            }
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

                    contact.Name.FullName = (contact.Name.GivenName + " " + contact.Name.FamilyName).Trim ();

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

            GDataContactExtensions.Merge (contact.Emails, contact.Emails, mail => mail.Address);
            contact.Emails.ForEach (mail => mail.Primary = contact.Emails.Count > 1 && mail.Address == contact.Emails.PrimaryAddress ());
            GDataContactExtensions.Merge (contact.Organizations, contact.Organizations, org => org.JobDescription + org.Title + org.Department + org.Name + org.Location);
            GDataContactExtensions.Merge (contact.Languages, contact.Languages, l => l.Value);
            GDataContactExtensions.Merge (contact.IMs, contact.IMs, l => l.Value);
            GDataContactExtensions.Merge (contact.Phonenumbers, contact.Phonenumbers, l => l.Value, GDataContactExtensions.UniqueFormat);
            GDataContactExtensions.Merge (contact.PostalAddresses, contact.PostalAddresses, a => a.City);

            Log.Debug ("Emails:", string.Join (", ", contact.Emails.Select (e => e.Address)));
            Log.Debug ("Organizations:", string.Join (", ", contact.Organizations.Select (org => org.JobDescription + org.Title + org.Department + org.Name + org.Location)));
            Log.Debug ("Languages:", string.Join (", ", contact.Languages.Select (l => l.Value)));
            Log.Debug ("IMs:", string.Join (", ", contact.IMs.Select (i => i.Value)));
            Log.Debug ("Phonenumbers:", string.Join (", ", contact.Phonenumbers.Select (p => p.Value + " (" + (p.Rel != null ? p.Rel : p.Label) + ")")));
            Log.Debug ("PostalAddresses:", string.Join ("; ", contact.PostalAddresses.Select (a => a.Format ())));


            if (string.Join (", ", contact.Emails.Select (e => e.Address)).Contains ("tobias.schulz.")
                || (contact.Name.FullName.ToLower ().Contains ("tobias") && !contact.Name.FullName.ToLower ().Contains ("schulz"))) {
                Log.Error ("Delete odd contact!!!");
                //ContactsRequest cr = new ContactsRequest (settings);
                //cr.Delete (contact);

                DeleteContact (contact);
            }


            CatchErrors (() => {
                ContactsRequest cr = new ContactsRequest (settings);
                cr.Update (contact);
            });
        }

        void DeleteContact (Contact contact)
        {
            contact.Name.FullName = "Unknown";
            contact.Name.GivenName = "Unknown";
            contact.Name.FamilyName = "";
            contact.Name.NamePrefix = "";
            contact.Name.NameSuffix = "";
            contact.Name.AdditionalName = "";
            contact.Emails.Clear ();
            contact.Organizations.Clear ();
            contact.Languages.Clear ();
            contact.IMs.Clear ();
            contact.Phonenumbers.Clear ();
            contact.PostalAddresses.Clear ();
            contact.ContactEntry.Relations.Clear ();
            contact.ContactEntry.Websites.Clear ();
            contact.ContactEntry.Nickname = "";
            contact.ContactEntry.ShortName = "";

            CatchErrors (() => {
                ContactsRequest cr = new ContactsRequest (settings);
                cr.Update (contact);
            });
        }
    }
}

