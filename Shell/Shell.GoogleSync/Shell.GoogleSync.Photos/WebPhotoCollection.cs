using System;
using System.Collections.Generic;
using System.Linq;
using Shell.Common.Util;
using Shell.GoogleSync.Photos;

namespace Shell.GoogleSync
{
    public class WebPhotoCollection
    {
        private List<WebPhoto> webFiles;
        private List<int> numResults;
        private List<object[]> messages;

        public WebPhoto[] WebFiles { get { return webFiles.ToArray (); } }

        public int[] NumResults { get { return numResults.ToArray (); } }

        public object[][] Messages { get { return messages.ToArray (); } }

        public WebPhotoCollection ()
        {
            webFiles = new List<WebPhoto> ();
            numResults = new List<int> ();
            messages = new List<object[]> ();
        }

        public void AddWebFile (WebPhoto file)
        {
            webFiles.Add (file);
        }

        public void RemoveWebFile (string name)
        {
            webFiles = webFiles.Where (f => f.Title != name).ToList ();
        }

        public void Log (params object[] message)
        {
            messages.Add (message);
        }

        public void AddCompletedQuery (int count)
        {
            numResults.Add (count);
        }
    }
}

