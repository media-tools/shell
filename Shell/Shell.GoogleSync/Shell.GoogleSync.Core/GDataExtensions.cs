using System;
using System.Collections.Generic;
using System.Linq;
using Google.Contacts;
using Google.GData.Client;
using Google.GData.Extensions;
using Shell.Common.IO;
using Shell.Common.Util;
using Shell.GoogleSync.Core;

namespace Shell.GoogleSync.Core
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

