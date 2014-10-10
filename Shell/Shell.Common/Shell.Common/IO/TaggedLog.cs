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
			return new object[] {
				"[" + tag + "]",
				String.Concat (Enumerable.Repeat (" ", MaxWidth - tag.Length - Log.IndentString.Length))
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
