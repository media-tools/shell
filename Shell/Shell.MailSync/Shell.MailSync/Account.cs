using System;
using Shell.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shell.MailSync
{
    public class Account
    {
        public string Accountname { get; private set; }

        public string Hostname { get; private set; }

        public string Username { get; private set; }

        public string Password { get; private set; }

        public Account (string accountname, string hostname, string username, string password)
        {
            Accountname = filterAccountname.Replace (accountname.ToLower (), "");
            Hostname = hostname;
            Username = username;
            Password = password;
        }

        public bool HasName (string name)
        {
            return Accountname == filterAccountname.Replace (name.ToLower (), "");
        }

        public override string ToString ()
        {
            return "Account(name=" + Accountname + ",host=" + Hostname + ",user=" + Username + ")"; //+ ",pass=" + String.Concat (Enumerable.Repeat ("?", Password.Length)) + ")";
        }

        private static Regex filterAccountname = new Regex ("[^a-zA-Z0-9]");
    }
}
