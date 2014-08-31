using System;
using System.Collections.Generic;
using System.Linq;
using Google.GData.Client;
using Google.GData.Extensions;

namespace Control.GoogleSync
{
    public static class GDataExtensions
    {
        public static OAuth2Parameters GetOAuth2Parameters (this GoogleAccount account)
        {
            GoogleApp appConfig = new GoogleApp ();

            OAuth2Parameters parameters = new OAuth2Parameters {
                ClientId = appConfig.ClientId,
                ClientSecret = appConfig.ClientSecret,
                RedirectUri = GoogleApp.RedirectUri,
                Scope = string.Join (" ", GoogleApp.Scopes),
            };
            parameters.AccessToken = account.AccessToken;
            parameters.RefreshToken = account.RefreshToken;

            return parameters;
        }

        public static string Join (this IEnumerable<EMail> mails, string separator)
        {
            return string.Join (separator, from mail in mails select mail.Address);
        }
    }
}

