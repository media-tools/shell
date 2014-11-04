﻿using System;
using Google.GData.Client;
using Shell.Common.Tasks;
using Shell.GoogleSync.Core;
using Shell.Common.IO;

namespace Shell.GoogleSync.Core
{
    public abstract class GDataLibrary : Library
    {
        // the google account
        protected GoogleAccount account;

        // for request-based libraries
        protected RequestSettings settings;

        // for service-based libraries
        protected GOAuth2RequestFactory requestFactory;


        public GDataLibrary (GoogleAccount account)
        {
            ConfigName = "Google";
            this.account = account;

            NetworkHelper.DisableCertificateChecks ();

            // get the OAuth2 parameters
            OAuth2Parameters parameters = account.GetOAuth2Parameters ();

            // for request-based libraries
            settings = new RequestSettings (GoogleApp.ApplicationName, parameters);

            // for service-based libraries
            requestFactory = new GOAuth2RequestFactory ("apps", GoogleApp.ApplicationName, parameters);
        }

        // use this contuctor only for accessing the file system variables !
        public GDataLibrary ()
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
    }
}
