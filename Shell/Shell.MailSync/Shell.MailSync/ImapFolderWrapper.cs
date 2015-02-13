using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;

namespace Shell.MailSync
{
	public class ImapFolderWrapper
	{
		private ImapClientWrapper client;

		public string FullName { get; private set; }

		private int ReconnectCounter = -1;

		public ImapFolderWrapper (ImapClientWrapper client, string fullName)
		{
			this.client = client;
			FullName = fullName;
			client.taggedLog.Debug ("wrap folder", "full name: ", FullName);
		}

		private IMailFolder internalFolder;

		private IMailFolder folder {
			get {
				if (ReconnectCounter != client.ReconnectCounter) {
					ReconnectCounter = client.ReconnectCounter;
					client.taggedLog.Debug ("get folder", "full name: ", FullName, ", reconnect counter: ", ReconnectCounter);
					internalFolder = client.GetInternalFolder (folder: FullName);
				}
				return internalFolder;
			}
		}

		/**
		 * see https://github.com/jstedfast/MailKit/issues/32#issuecomment-58703460
		 */
		public MessageList GetMessages ()
		{
			MessageList messages = new MessageList ();
			client.Try (() => {
				foreach (var summary in from msg in folder.Fetch (0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Flags | MessageSummaryItems.InternalDate | MessageSummaryItems.MessageSize | MessageSummaryItems.Envelope) orderby msg.Index select msg) {
					//Console.WriteLine ("[summary] {0:D2}: {1} {2} {3}", summary.Index, summary.Envelope.Subject, summary.SortableDate, key(summary));
					messages.Add (summary);
				}
			});
			return messages;
		}

		public void Open ()
		{
			client.Try (() => {
				folder.Open (FolderAccess.ReadWrite);
			});
			client.CurrentFolder = FullName;
		}

		// deletegate everything else to MailKit.IMailFolder

		public int Count {
			get {
				int count = 0;
				client.Try (() => {
					count = folder.Count;
				});
				return count;
			}
		}

		public void Expunge ()
		{
			client.Try (() => {
				folder.Expunge ();
			});
		}

		public IMailFolder Create (string name, bool isMessageFolder, CancellationToken cancellationToken = default (CancellationToken))
		{
			IMailFolder result = null;
			client.Try (() => {
				result = folder.Create (name, isMessageFolder, cancellationToken);
			});
			return result;
		}

		public IEnumerable<ImapFolderWrapper> GetSubfolders (bool subscribedOnly = false, CancellationToken cancellationToken = default (CancellationToken))
		{
			IEnumerable<ImapFolderWrapper> folders = new ImapFolderWrapper[0];
			client.Try (() => {
				folders = from f in folder.GetSubfolders (subscribedOnly, cancellationToken)
				          select new ImapFolderWrapper (client: client, fullName: f.FullName);
			});
			return folders;
		}

		public void AddFlags (IEnumerable<UniqueId> uids, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken))
		{
			client.Try (() => {
				folder.AddFlags (uids.ToList (), flags, silent, cancellationToken);
			});
		}

		public MimeKit.MimeMessage GetMessage (UniqueId uid, CancellationToken cancellationToken = default (CancellationToken))
		{
			MimeKit.MimeMessage message = null;
			client.Try (() => {
				message = folder.GetMessage (uid, cancellationToken);
			});
			return message;
		}

		public IList<IMessageSummary> Fetch (IEnumerable<UniqueId> uids, MessageSummaryItems items, CancellationToken cancellationToken = default (CancellationToken))
		{
			IList<IMessageSummary> list = null;
			client.Try (() => {
				list = folder.Fetch (uids.ToList (), items, cancellationToken);
			});
			return list;
		}

		public Nullable<UniqueId> Append (MimeKit.MimeMessage message, MessageFlags flags, DateTimeOffset date, CancellationToken cancellationToken = default (CancellationToken))
		{
			UniqueId? uid = null;
			client.Try (() => {
				uid = folder.Append (message, flags, date, cancellationToken);
			});
			return uid;
		}
	}
}

