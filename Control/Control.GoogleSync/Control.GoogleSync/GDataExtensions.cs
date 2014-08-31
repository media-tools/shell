using System;
using Google.GData.Client;

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
    }
}

