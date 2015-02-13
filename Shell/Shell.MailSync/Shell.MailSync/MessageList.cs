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
                msgid = ""; // gmx seems to have no message id sometimes
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


        public static IEnumerable<ImapFolderWrapper> GetAllFolders (this ImapClientWrapper client, bool includeTrash)
        {
            List<ImapFolderWrapper> folders = new List<ImapFolderWrapper> ();
            client.Try (() => {
                foreach (FolderNamespace ns in client.PersonalNamespaces) {
                    ImapFolderWrapper rootFolder = client.GetFolder (ns);
                    foreach (ImapFolderWrapper f in rootFolder.GetAllSubFolders(includeTrash)) {
                        folders.Add (f);
                    }
                }
            });
            return folders;
        }

        private static IEnumerable<ImapFolderWrapper> GetAllSubFolders (this ImapFolderWrapper folder, bool includeTrash)
        {
            if (folder.FullName.Contains ("Papierkorb") || folder.FullName.Contains ("Trash") || folder.FullName.Contains ("Spam")) {
                yield break;
            }
            int i = 0;
            foreach (ImapFolderWrapper subFolder in folder.GetSubfolders ()) {
                foreach (ImapFolderWrapper f in subFolder.GetAllSubFolders(includeTrash)) {
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

