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

		public static string KEY (IMessageSummary summary)
		{
			string msgid = summary.Envelope.MessageId ?? "";
			if (msgid.Contains ("unknownmsgid")) {
				msgid = "unknown";
			}

			string subject = summary.Envelope.Subject ?? "";
			subject = subject.ToLower ().Trim ();

			string sender = null;
			if (sender == null && summary.Envelope.From.Count > 0) {
				sender = summary.Envelope.From.First ().Name ?? "";
			}
			if (sender == null && summary.SortableFrom != null) {
				sender = summary.SortableFrom;
			}
			if (sender != null && sender.Contains ("reply")) {
				sender = null;
			}
			sender = sender ?? "";

			return string.Join (";", msgid, subject, sender);
		}

		public void Add (IMessageSummary summary)
		{
			if (summary.Envelope != null && KEY (summary) != null) {
				messages [KEY (summary)] = summary;
			}
		}

		public bool Contains (IMessageSummary summary)
		{
			return messages.ContainsKey (KEY (summary));
			
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
		/**
		 * see https://github.com/jstedfast/MailKit/issues/32#issuecomment-58703460
		 */
		public static MessageList GetMessages (this IMailFolder folder)
		{
			MessageList messages = new MessageList ();
			foreach (var summary in from msg in folder.Fetch (0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Flags | MessageSummaryItems.InternalDate | MessageSummaryItems.MessageSize | MessageSummaryItems.Envelope) orderby msg.Index select msg) {
				//Console.WriteLine ("[summary] {0:D2}: {1} {2} {3}", summary.Index, summary.Envelope.Subject, summary.SortableDate, key(summary));
				messages.Add (summary);
			}
			return messages;
		}

		public static IMailFolder GetFolderOrCreate (this ImapClient client, string path)
		{
			//if (path == SpecialFolder.Trash+"") {
			//	return client.GetAllFolders (includeTrash: true).Where (f => f.FullName.Contains ("Papierkorb") || f.FullName.Contains ("Trash")).First ();
			//}
			SpecialFolder[] specialFolders = new SpecialFolder[] {
				SpecialFolder.Trash,
				SpecialFolder.Sent,
				SpecialFolder.Junk,
				SpecialFolder.Drafts,
				SpecialFolder.Archive
			};
			foreach (SpecialFolder specialFolder in specialFolders) {
				if (path == specialFolder + "") {
					return client.GetFolder (specialFolder);
				}
			}

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

		public static IEnumerable<IMailFolder> GetAllFolders (this ImapClient client, bool includeTrash)
		{
			foreach (FolderNamespace ns in client.PersonalNamespaces) {
				IMailFolder rootFolder = client.GetFolder (ns);
				foreach (IMailFolder f in rootFolder.GetAllSubFolders(includeTrash)) {
					yield return f;
				}
			}
		}

		private static IEnumerable<IMailFolder> GetAllSubFolders (this IMailFolder folder, bool includeTrash)
		{
			if (folder.FullName.Contains ("Papierkorb") || folder.FullName.Contains ("Trash") || folder.FullName.Contains ("Spam")) {
				yield break;
			}
			int i = 0;
			foreach (IMailFolder subFolder in folder.GetSubfolders ()) {
				foreach (IMailFolder f in subFolder.GetAllSubFolders(includeTrash)) {
					yield return f;
				}
				++i;
			}
			if (i == 0) {
				yield return folder;
			}
		}

		public static string Print (this IMessageSummary summary)
		{
			string msgid = summary.Envelope.MessageId ?? string.Empty;
			if (msgid.Contains ("unknownmsgid")) {
				msgid = ""; // gmx seems to have no message id sometimes
			}

			string subject = summary.Envelope.Subject ?? string.Empty;
			subject = subject.ToLower ().Trim ();

			string sender = null;
			if (sender == null && summary.Envelope.From.Count > 0) {
				sender = summary.Envelope.From.First ().Name ?? string.Empty;
			}
			if (sender == null && summary.SortableFrom != null) {
				sender = summary.SortableFrom;
			}
			if (sender != null && sender.Contains ("reply")) {
				sender = null;
			}
			sender = sender ?? "";

			DateTimeOffset date = summary.SortableDate;

			return string.Format ("#{0:D2}: subject=[{1}] date=[{2}] msgid=[{3}]", summary.Index, subject, date, msgid);
		}
	}
}

