using System;
using Shell.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shell.MailSync
{
	public class Channel
	{
		public Account FromAccount { get; private set; }

		public Account ToAccount { get; private set; }

		public string FromPath { get; private set; }

		public string ToPath { get; private set; }

		public ChannelOperation Operation { get; private set; }

		public Dictionary<string,string> Parameters { get; private set; }

		//public string FromFullName { get { return FromAccount.Accountname + ":" + FromPath; } }
		//public string ToFullName { get { return ToAccount.Accountname + ":" + ToPath; } }

		public Channel (List<Account> accounts, string from, string to, ChannelOperation op, Dictionary<string,string> parameters)
		{
			Operation = op;
			Parameters = parameters;
			string[] _from = from.Split (':');
			string[] _to = to.Split (':');
			if (_from.Length != 2) {
				throw new ArgumentException ("Invalid channel source string: " + from);
			}
			if (_to.Length != 2) {
				throw new ArgumentException ("Invalid channel target string: " + to);
			}
			Account fromAccount;
			Account toAccount;
			if (!findAccount (accounts, _from [0], out fromAccount)) {
				throw new ArgumentException ("Invalid channel source account: " + from);
			}
			if (!findAccount (accounts, _to [0], out toAccount)) {
				throw new ArgumentException ("Invalid channel target account: " + from);
			}
			FromAccount = fromAccount;
			ToAccount = toAccount;
			FromPath = _from [1];
			ToPath = _to [1];
		}

		private bool findAccount (List<Account> accounts, string name, out Account rightAcc)
		{
			foreach (Account acc in accounts) {
				if (acc.HasName (name)) {
					rightAcc = acc;
					return true;
				}
			}
			rightAcc = null;
			return false;
		}

		public static ChannelOperation ParseOperation (string txt)
		{
			if (txt.ToLower () == "copy") {
				return ChannelOperation.COPY;
			} else if (txt.ToLower () == "move") {
				return ChannelOperation.MOVE;
			} else {
				throw new ArgumentException ("Invalid operation: " + txt);
			}
		}

		public override string ToString ()
		{
			return "Channel(op=" + Operation + ",from=" + FromAccount.Accountname + ":" + FromPath + ",to=" + ToAccount.Accountname + ":" + ToPath +
			string.Join ("", Parameters.Select (pair => "," + pair.Key + "=" + pair.Value)) + ")";
		}
	}

	public enum ChannelOperation
	{
		COPY,
		MOVE
	}
}
