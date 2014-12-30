using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.IO;
using Shell.Common.Util;
using Shell.GoogleSync.Core;
using Shell.Media;

namespace Shell.GoogleSync
{
    public class GoogleShareManager
    {
        private List<Tuple<MediaShare, GoogleAccount>> Mapping;

        public GoogleShareManager (MediaShareManager shareManager)
        {
            Mapping = new List<Tuple<MediaShare, GoogleAccount>> ();

            init (shareManager: shareManager, accounts: GoogleAccount.List ().ToArray ());
        }

        private void init (MediaShareManager shareManager, GoogleAccount[] accounts)
        {
            foreach (MediaShare share in from share in shareManager.SharesByConfigFile.Values orderby share.RootDirectory select share) {
                // if there is a valid google account config value
                if (!string.IsNullOrWhiteSpace (share.GoogleAccount)) {
                    GoogleAccount[] matches = accounts.Where (a => a.Emails.Replace (".", "").ToLower ().Contains (share.GoogleAccount.Replace (".", "").ToLower ())).ToArray ();

                    // if there are no google accounts matching the value
                    if (matches.Length == 0) {
                        Log.Debug ("Share: ", share.Name, ", Google Account: none.");
                    }
                    // if there are more than one matching google accounts 
                    else if (matches.Length >= 2) {
                        Log.Debug ("Share: ", share.Name, ", Google Account: multiple: ", string.Join (", ", matches.Select (a => a.DisplayName + " <" + a.Emails + ">")));
                    }
                    // if there is exactly one matching google account!
                    else {
                        GoogleAccount account = matches [0];
                        Log.Debug ("Share: ", share.Name, ", Google Account: ", account);

                        Mapping.Add (Tuple.Create (share, account));
                    }
                }
            }
        }

        public bool Contains (GoogleAccount account)
        {
            return Mapping.Any (tuple => tuple.Item2 == account);
        }

        public bool Contains (MediaShare share)
        {
            return Mapping.Any (tuple => tuple.Item1 == share);
        }

        public MediaShare this [GoogleAccount account] {
            get {
                foreach (Tuple<MediaShare, GoogleAccount> tuple in Mapping) {
                    if (account == tuple.Item2) {
                        return tuple.Item1;
                    }
                }
                return null;
            }
        }

        public GoogleAccount this [MediaShare share] {
            get {
                foreach (Tuple<MediaShare, GoogleAccount> tuple in Mapping) {
                    if (share == tuple.Item1) {
                        return tuple.Item2;
                    }
                }
                return null;
            }
        }

        public MediaShare[] Shares { get { return Mapping.Select (tuple => tuple.Item1).ToArray (); } }

        public void PrintShares (Filter shareFilter, Filter googleUserFilter)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter).FilterBy (googleUserFilter, share => this [share]);

            if (filteredShares.Length != 0) {
                Log.Message ("List of local picture shares:");
                Log.Indent++;
                Log.Message (filteredShares.OrderBy (s => s.RootDirectory).ToStringTable (
                    s => LogColor.Reset,
                    new[] { "Name", "Root Directory", "Album Count", "Google Name", "Google Email", "Google ID" },
                    s => s.Name,
                    s => s.RootDirectory,
                    s => s.Albums.Count,
                    s => this [s].DisplayName,
                    s => this [s].Emails,
                    s => this [s].Id
                ));
                Log.Indent--;
            } else {
                Log.Message ("No shares available.");
            }
        }


        public void PrintAlbums (Filter shareFilter, Filter googleUserFilter, Filter albumFilter)
        {
            MediaShare[] filteredShares = Shares.Filter (shareFilter).FilterBy (googleUserFilter, share => this [share]);

            if (filteredShares.Length != 0) {
                Log.Message ("List of local picture shares:");
                Log.Indent++;
                int i = 1;
                foreach (MediaShare share in filteredShares.OrderBy (share => share.RootDirectory)) {
                    share.PrintAlbums (albumFilter: albumFilter);
                    i++;
                }
                Log.Indent--;
            } else {
                Log.Message ("No shares available.");
            }
        }
    }
}

