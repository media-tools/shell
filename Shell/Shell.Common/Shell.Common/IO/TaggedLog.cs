using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Shell.Common.Util;

namespace Shell.Common.IO
{
	public class TaggedLog
	{
		public int MaxWidth;

		public TaggedLog (int maxWidth)
		{
			MaxWidth = maxWidth;
		}

		private object[] construstTaggedMessage (string tag, params object[] message)
		{
			string tagString = tag.Length > 0 ? "[" + tag + "]" : string.Empty;

			return new object[] {
				tagString,
				String.Concat (Enumerable.Repeat (" ", Math.Max (0, MaxWidth - tagString.Length - Log.IndentString.Length)))
			}.Concat (message).ToArray ();
		}

		public void Message (string tag, params object[] message)
		{
			object[] taggedMessage = construstTaggedMessage (tag, message);
			Log.MessageConsole (taggedMessage);
			Log.MessageLog (taggedMessage);
		}

		public void Debug (string tag, params object[] message)
		{
			object[] taggedMessage = construstTaggedMessage (tag, message);
			Log.DebugConsole (taggedMessage);
			Log.DebugLog (taggedMessage);
		}
	}
}
