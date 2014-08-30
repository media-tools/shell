using System;
using Control.Common.IO;
using Control.Common;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Plus.v1;
using System.Threading;
using Google.Apis.Services;
using Google.Apis.Plus.v1.Data;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

namespace Control.GoogleSync
{
    public class GoogleAppConfig : Library
    {
        public string ClientId { get; private set; }

        public string ClientSecret { get; private set; }

        public ClientSecrets Secrets { get; private set; }


        public static readonly string ApplicationName = "Control.Google";
        // Installed (non-web) application
        public static string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        // Requesting access to Contacts API
        private static IEnumerable<string> scopes = new[] {
            PlusService.Scope.PlusLogin,
            PlusService.Scope.UserinfoProfile,
            PlusService.Scope.UserinfoEmail
        };
        public List<GoogleAccount> Accounts = new List<GoogleAccount> ();

        public GoogleAppConfig ()
        {
            ConfigName = "Google";
            ConfigFile appConfig = fs.Config.OpenConfigFile ("app.ini");
            Secrets = new ClientSecrets () {
                ClientId = appConfig ["GoogleApp", "client_id", "574696664370-fjvhqijeokikvqblaqmf30hpk9g280r4.apps.googleusercontent.com"],
                ClientSecret = appConfig ["GoogleApp", "client_secret", "6RPbdpnnaCDgJr3iNTmpgmjT"]
            };
        }

        public bool Authenticate ()
        {
            return Authenticate (account: null);
        }

        public bool Authenticate (GoogleAccount account)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;

            DictionaryDataStore dataStore = new DictionaryDataStore ();

            if (account != null) {
                account.LoadDataStore (dataStore: ref dataStore);
            }

            try {
                UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync (
                                                clientSecrets: Secrets,
                                                scopes: scopes,
                                                user: "me",
                                                taskCancellationToken: CancellationToken.None,
                                                dataStore: dataStore
                                            ).Result;
                Accounts.Add (GoogleAccount.SaveAccount (credential: credential, dataStore: dataStore));
                return true;
            } catch (TokenResponseException ex) {
                Log.Error (ex);
                return false;
            }
        }
    }
}

