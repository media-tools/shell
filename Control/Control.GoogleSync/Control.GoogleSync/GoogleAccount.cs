using System;
using Control.Common;
using Control.Common.IO;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Plus.v1;
using Google.Apis.Services;
using Google.Apis.Plus.v1.Data;
using System.Collections.Generic;

namespace Control.GoogleSync
{
    public class GoogleAccount : Library
    {
        private ConfigFile accountConfig;

        private string section;

        public string Id { get { return accountConfig [section, "Id", ""]; } }

        public string DisplayName { get { return accountConfig [section, "DisplayName", ""]; } }

        public string Emails { get { return accountConfig [section, "Emails", ""]; } }

        public string Url { get { return accountConfig [section, "Url", ""]; } }

        public string AccessToken { get { return accountConfig [section, "AccessToken", ""]; } }

        public string RefreshToken { get { return accountConfig [section, "RefreshToken", ""]; } }

        public GoogleAccount (string id)
            : this ()
        {
            section = id2section (id);
            string displayName = accountConfig [section, "DisplayName", ""];
            string emails = accountConfig [section, "Emails", ""];
        }

        private GoogleAccount ()
        {
            ConfigName = "Google";
            accountConfig = fs.Config.OpenConfigFile ("accounts.ini");
        }

        public static GoogleAccount SaveAccount (UserCredential credential, DictionaryDataStore dataStore)
        {
            GoogleAccount dummy = new GoogleAccount ();

            // Create the service.
            PlusService plusService = new PlusService (new BaseClientService.Initializer () {
                HttpClientInitializer = credential,
                ApplicationName = GoogleApp.ApplicationName,
            });

            Person me = plusService.People.Get ("me").Execute ();

            string id = me.Id;
            string section = id2section (id);
            Log.Message ("Authorized user: ", me.DisplayName);

            ConfigFile accountConfig = dummy.accountConfig;

            accountConfig ["General", "account_list", ""] = string.Join (";", new HashSet<string> (accountConfig ["General", "account_list", ""].Split (new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Concat (new string[] { id })));
            accountConfig [section, "AccessToken", ""] = credential.Token.AccessToken;
            accountConfig [section, "RefreshToken", ""] = credential.Token.RefreshToken;
            accountConfig [section, "Id", ""] = me.Id;
            accountConfig [section, "Emails", ""] = string.Join (";", from email in me.Emails ?? new Person.EmailsData[0]
                                                                               select email.Value);
            accountConfig [section, "DisplayName", ""] = me.DisplayName;
            accountConfig [section, "Url", ""] = me.Url;
            accountConfig [section, "RelationshipStatus", ""] = me.RelationshipStatus;
            accountConfig [section, "Image.Url", ""] = me.Image.Url;

            dataStore.Save (configFile: accountConfig, section: section);

            return new GoogleAccount (id: id);
        }

        public static IEnumerable<GoogleAccount> List ()
        {
            GoogleAccount dummy = new GoogleAccount ();
            ConfigFile accountConfig = dummy.fs.Config.OpenConfigFile ("accounts.ini");

            string[] ids = accountConfig ["General", "account_list", ""].Split (new string[]{ ";" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string id in ids) {
                GoogleAccount acc = new GoogleAccount (id: id);
                yield return acc;
            }
        }

        public void LoadDataStore (ref DictionaryDataStore dataStore)
        {
            dataStore.Load (configFile: accountConfig, section: section);
        }

        private static string id2section (string id)
        {
            return "Account_" + id;
        }

        public bool Reauthenticate ()
        {
            Log.Message (LogColor.DarkYellow, "Google Account needs to be re-authenticated: ", this, LogColor.Reset);
            return new GoogleApp().Authenticate ();
        }

        public override string ToString ()
        {
            return string.Format ("{0} <{1}> ({2})", DisplayName, Emails, Id);
        }

        /*

            try {
                RequestSettings settings = new RequestSettings (GoogleAppConfig.ApplicationName, parameters);
                ContactsRequest cr = new ContactsRequest (settings);

                Feed<Contact> f = cr.GetContacts ();
                foreach (Contact c in f.Entries) {
                    Console.WriteLine (c.Name.FullName);
                }
            } catch (Exception ex) {
                Log.Error (ex);
            }*/
    }
}

