using System;
using Control.Common.IO;
using Google.GData.Client;
using Control.Common;
using System.Collections.Generic;

namespace Control.GoogleSync
{
    public class GoogleAppConfig : Library
    {
        public string ClientId { get; private set; }

        public string ClientSecret { get; private set; }

        public static readonly string ApplicationName = "Control.Google";
        // Installed (non-web) application
        public static string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        // Requesting access to Contacts API
        private static string scopes = "https://www.google.com/m8/feeds/";
        public List<GoogleAccount> Accounts = new List<GoogleAccount> ();

        public GoogleAppConfig ()
        {
            ConfigName = "Google";
            ConfigFile appConfig = fs.Config.OpenConfigFile ("app.ini");
            ClientId = appConfig ["GoogleApp", "client_id", "574696664370-fjvhqijeokikvqblaqmf30hpk9g280r4.apps.googleusercontent.com"];
            ClientSecret = appConfig ["GoogleApp", "client_secret", "6RPbdpnnaCDgJr3iNTmpgmjT"];
        }

        public void Authenticate ()
        {




            OAuth2Parameters parameters = new OAuth2Parameters () {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                RedirectUri = RedirectUri,
                Scope = scopes
            };

            string url = OAuthUtil.CreateOAuth2AuthorizationUrl (parameters);
            Log.MessageConsole ("Authorize URI: ", LogColor.DarkBlue, url, LogColor.Reset);

            bool done = false;
            do {
                Console.Write ("Access Code: ");
                parameters.AccessCode = Console.ReadLine ();
                
                Console.Write ("Account Name: ");
                string accountName = Console.ReadLine ();

            
                if (string.IsNullOrWhiteSpace (parameters.AccessCode)) {
                    done = false;
                    Log.Error ("The access code is invalid.");
                } else if (string.IsNullOrWhiteSpace (accountName)) {
                    done = false;
                    Log.Error ("The account name is invalid.");
                } else {
                    OAuthUtil.GetAccessToken (parameters);
                    Accounts.Add (new GoogleAccount (name: accountName, parameters: parameters));
                    done = true;
                }
            } while (!done);
        }
    }
}

