using System;
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

        // internal lock
        private object internalLockAuth = new object ();

        public GDataLibrary (GoogleAccount account)
        {
            ConfigName = "Google";
            this.account = account;

            NetworkHelper.DisableCertificateChecks ();

            UpdateAuthInternal ();
        }

        private void UpdateAuthInternal ()
        {
            lock (internalLockAuth) {
                // get the OAuth2 parameters
                OAuth2Parameters parameters = account.GetOAuth2Parameters ();

                // for request-based libraries
                settings = new RequestSettings (GoogleApp.ApplicationName, parameters);

                // for service-based libraries
                requestFactory = new GOAuth2RequestFactory ("apps", GoogleApp.ApplicationName, parameters);
                requestFactory.MethodOverride = true;

                // call the method implemented in the derived class
                UpdateAuth ();
            }
        }

        protected abstract void UpdateAuth ();

        // use this contuctor only for accessing the file system variables !
        public GDataLibrary ()
        {
            ConfigName = "Google";
        }

        public void CatchErrors (Action todo)
        {
            string dummy;
            CatchErrors (todo: todo, errorMessage: out dummy, catchAllExceptions: false, retryTimes: 1);
        }

        public void CatchErrors (Action todo, out string errorMessage, bool catchAllExceptions, int retryTimes)
        {
            int triesLeft = retryTimes;
            do {
                try {
                    todo ();
                    errorMessage = null;
                } catch (GDataRequestException ex) {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains ("wrong scope")) {
                        Log.Error ("GDataRequestException: ", ex.ResponseString);
                        account.Reauthenticate ();
                        todo ();
                    } else {
                        Log.Error ("GDataRequestException: ", ex.ResponseString);
                        Log.Error (ex);
                        if (ex.ResponseString.Contains ("Token invalid")) {
                            Log.Debug ("Refresh account authorization:");
                            Log.Indent++;
                            account.Refresh ();
                            UpdateAuthInternal ();
                            Log.Indent--;
                        }
                        // Log.Error (ex);
                    }
                    errorMessage = ex.ResponseString;
                } catch (ClientFeedException ex) {
                    Log.Error ("ClientFeedException: ", ex.InnerException);
                    Log.Error (ex);
                    errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                } catch (Exception ex) {
                    errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    if (catchAllExceptions) {
                        Log.Error (ex);
                    } else {
                        throw;
                    }
                }
            } while (errorMessage != null && triesLeft-- > 0);
        }
    }
}

