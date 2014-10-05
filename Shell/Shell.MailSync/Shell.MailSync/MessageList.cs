using System;
using System.Collections.Generic;
using MailKit;
using Shell.Common.IO;
using MailKit.Net.Imap;
using System.Linq;

namespace Shell.MailSync
{
	public class MessageList : IEnumerable<IMessageSummary>
	{
		private Dictionary<string, IMessageSummary> messages = new Dictionary<string, IMessageSummary> ();

		public MessageList ()
		{
		}

		private string key (IMessageSummary summary)
		{
			//return summary.Envelope.MessageId;
			//Console.WriteLine ((summary.Envelope.Date.HasValue ? summary.Envelope.Date.Value.ToString () : "")
			//+ summary.Envelope.Subject
			//+ (summary.Envelope.From.Count > 0 ? summary.Envelope.From.First ().Name : ""));
			return (summary.Envelope.Date.HasValue ? summary.Envelope.Date.Value.ToString () : "")
			+ summary.Envelope.Subject
			+ (summary.Envelope.From.Count > 0 ? summary.Envelope.From.First ().Name : "");
		}

		public void Add (IMessageSummary summary)
		{
			if (summary.Envelope != null && key (summary) != null) {
				messages [key (summary)] = summary;
			}
		}

		public bool Contains (IMessageSummary summary)
		{
			return messages.ContainsKey (key (summary));
		}

		public MessageList Intersect (MessageList other)
		{
			MessageList result = new MessageList ();
			foreach (IMessageSummary summary in messages.Values) {
				if (other.Contains (summary)) {
					result.Add (summary);
				}
			}
			return result;
		}

		public MessageList Except (MessageList other)
		{
			MessageList result = new MessageList ();
			foreach (IMessageSummary summary in messages.Values) {
				if (!other.Contains (summary)) {
					result.Add (summary);
				}
			}
			return result;
		}

		public int Count { get { return messages.Count; } }

		public IEnumerator<IMessageSummary> GetEnumerator ()
		{
			return messages.Values.GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return messages.Values.GetEnumerator ();
		}
	}

	public static class MessageListExtensions
	{
		public static MessageList GetMessages (this IMailFolder folder)
		{
			MessageList messages = new MessageList ();
			foreach (var summary in folder.Fetch (0, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId)) {
				//Console.WriteLine ("[summary] {0:D2}: {1} {2} {3}", summary.Index, summary.Envelope.Subject, summary.SortableDate, key(summary));
				messages.Add (summary);
			}
			return messages;
		}

		public static IMailFolder GetFolderOrCreate (this ImapClient client, string path)
		{
			IMailFolder folder = client.GetFolder (path);
			if (folder == null) {
				Log.Debug ("namespaces: ", string.Join (",", client.PersonalNamespaces.Select (ns => ns.Path)));
				client.GetFolder (client.PersonalNamespaces [0]).Create (name: path, isMessageFolder: true);
				folder = client.GetFolder (path);
			}
			if (folder == null) {
				throw new ArgumentException ("Can't create folder: ", path);
			}
			return folder;
		}
	}
}

