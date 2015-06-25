using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Plus.v1;
using Google.Apis.Plus.v1.Data;
using Google.Apis.Services;
using Shell.Common.IO;
using Shell.Common.Tasks;
using Shell.Common.Util;
using Shell.Namespaces;
using Newtonsoft.Json;
using Core.IO;

namespace Shell.GoogleSync.Core
{
    public class GoogleAccount : ConfigurableObject, IFilterable
    {
        ConfigFile accountConfig;
        GoogleAccountListJson jsonAccounts;

        string section;

        public string Id { get { return accountConfig [section, "Id", ""]; } }

        public string DisplayName { get { return accountConfig [section, "DisplayName", ""]; } }

        static Regex filterShortDisplayName = new Regex ("[^a-z]");

        public string ShortDisplayName { get { return filterShortDisplayName.Replace (FirstName.ToLower (), ""); } }

        public string FirstName { get { return DisplayName.Contains (" ") ? DisplayName.Substring (0, DisplayName.IndexOf (" ")) : DisplayName; } }

        public string Emails { get { return accountConfig [section, "Emails", ""]; } }

        public string Url { get { return accountConfig [section, "Url", ""]; } }

        public string AccessToken { get { return accountConfig [section, "AccessToken", ""]; } private set { accountConfig [section, "AccessToken", ""] = value; } }

        public string RefreshToken { get { return accountConfig [section, "RefreshToken", ""]; } }

        public GoogleAccount (string id)
            : this ()
        {
            section = id2section (id);

            if (!jsonAccounts.Accounts.ContainsKey (id)) {
                jsonAccounts.Accounts [id] = new GoogleAccountJson ();
            }
        }

        private GoogleAccount ()
        {
            ConfigName = NamespaceGoogle.CONFIG_NAME;
            accountConfig = fs.Config.OpenConfigFile ("accounts.ini");

            string content = fs.Config.ReadAllText ("accounts.json");
            jsonAccounts = PortableConfigHelper.ReadConfig<GoogleAccountListJson> (content: ref content);
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

            accountConfig ["General", "account_list", ""] = accountConfig ["General", "account_list", ""].SplitValues ().Concat (id).JoinValues ();
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

            GoogleAccountListJson jsonAccounts = dummy.jsonAccounts;
            if (!jsonAccounts.Accounts.ContainsKey (id)) {
                jsonAccounts.Accounts [id] = new GoogleAccountJson ();
            }

            GoogleAccountJson accJson = jsonAccounts.Accounts [id];
            accJson.AccessToken = credential.Token.AccessToken;
            accJson.RefreshToken = credential.Token.RefreshToken;
            accJson.Id = me.Id;
            accJson.Emails = (from email in me.Emails ?? new Person.EmailsData[0]
                                       select email.Value).ToArray ();
            accJson.DisplayName = me.DisplayName;

            dummy.fs.Config.WriteAllText (path: "accounts.json", contents: PortableConfigHelper.WriteConfig (stuff: jsonAccounts));

            return new GoogleAccount (id: id);
        }

        public static IEnumerable<GoogleAccount> List ()
        {
            GoogleAccount dummy = new GoogleAccount ();
            ConfigFile accountConfig = dummy.fs.Config.OpenConfigFile ("accounts.ini");

            string[] ids = accountConfig ["General", "account_list", ""].SplitValues ();
            foreach (string id in ids) {
                GoogleAccount acc = new GoogleAccount (id: id);
                yield return acc;


                GoogleAccountListJson jsonAccounts = dummy.jsonAccounts;
                if (!jsonAccounts.Accounts.ContainsKey (id)) {
                    jsonAccounts.Accounts [id] = new GoogleAccountJson ();
                }

                GoogleAccountJson accJson = jsonAccounts.Accounts [id];
                accJson.AccessToken = accountConfig [acc.section, "AccessToken", ""];
                accJson.RefreshToken = accountConfig [acc.section, "RefreshToken", ""];
                accJson.Id = accountConfig [acc.section, "Id", ""];
                accJson.Emails = accountConfig [acc.section, "Emails", ""].Split (';');
                accJson.DisplayName = accountConfig [acc.section, "DisplayName", ""];

                DictionaryDataStore dds = new DictionaryDataStore ();
                dds.Load (configFile: accountConfig, section: acc.section);
                dds.Save (dictionary: accJson.DataStore);

                dummy.fs.Config.WriteAllText (path: "accounts.json", contents: PortableConfigHelper.WriteConfig (stuff: jsonAccounts));
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

        public bool Refresh ()
        {
            NetworkHelper.DisableCertificateChecks ();

            TokenResponse token = new TokenResponse {
                AccessToken = AccessToken,
                RefreshToken = RefreshToken
            };

            IAuthorizationCodeFlow flow =
                //new GoogleAuthorizationCodeFlow (new GoogleAuthorizationCodeFlow.Initializer {
                new AuthorizationCodeFlow (new AuthorizationCodeFlow.Initializer (Google.Apis.Auth.OAuth2.GoogleAuthConsts.AuthorizationUrl, Google.Apis.Auth.OAuth2.GoogleAuthConsts.TokenUrl) {
                    ClientSecrets = new GoogleApp ().Secrets,
                    Scopes = new [] { PlusService.Scope.PlusLogin }
                });

            UserCredential credential = new UserCredential (flow, "me", token);
            bool success;
            try {
                success = credential.RefreshTokenAsync (CancellationToken.None).Result;
            } catch (AggregateException ex) {
                Log.Error ("RefreshTokenAsync failed: ");
                Log.Indent++;
                Log.Error (ex);
                Log.Indent--;
                success = false;
            }

            if (success) {
                token = credential.Token;
                AccessToken = token.AccessToken;
                Log.Debug ("Refresh successful: ", AccessToken);
                return true;
            } else {
                Log.Error ("Refresh failed: ", this);
                return false;
            }
        }

        public bool Reauthenticate ()
        {
            Log.Message (LogColor.DarkYellow, "Google Account needs to be re-authenticated: ", this, LogColor.Reset);
            return new GoogleApp ().Authenticate ();
        }

        public string[] FilterKeys ()
        {
            return new [] { DisplayName, Emails };
        }

        public override string ToString ()
        {
            return string.Format ("{0} <{1}> ({2})", DisplayName, Emails, Id);
        }


        protected override IEnumerable<object> Reflect ()
        {
            return new object[] { Id };
        }

        public override bool Equals (object obj)
        {
            return ValueObject<ConfigurableObject>.Equals (myself: this, obj: obj);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public static bool operator == (GoogleAccount a, GoogleAccount b)
        {
            return ValueObject<ConfigurableObject>.Equality (a, b);
        }

        public static bool operator != (GoogleAccount a, GoogleAccount b)
        {
            return ValueObject<ConfigurableObject>.Inequality (a, b);
        }

        public sealed class GoogleAccountJson
        {
            [JsonProperty ("access_token")]
            public string AccessToken { get; set; } = "";

            [JsonProperty ("refresh_token")]
            public string RefreshToken { get; set; } = "";

            [JsonProperty ("id")]
            public string Id { get; set; } = "";

            [JsonProperty ("emails")]
            public string[] Emails { get; set; } = new string[0];

            [JsonProperty ("display_name")]
            public string DisplayName { get; set; } = "";

            [JsonProperty ("data_store")]
            public Dictionary<string, string> DataStore { get; set; } = new Dictionary<string, string>();
        }

        public sealed class GoogleAccountListJson
        {
            [JsonProperty ("accounts")]
            public Dictionary<string, GoogleAccountJson> Accounts { get; set; } = new Dictionary<string, GoogleAccountJson>();
        }
    }
}

