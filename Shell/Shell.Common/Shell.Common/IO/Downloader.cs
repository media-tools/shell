using System;
using System.Net;
using System.ComponentModel;
using System.Diagnostics;

namespace Shell.GoogleSync
{
    public class Downloader
    {
        WebClient webClient;
        Stopwatch sw = new Stopwatch ();

        public Downloader ()
        {
        }

        public void DownloadFile (string localPath, string url)
        {
            using (WebClient webClient = new WebClient ()) {
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler (Completed);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler (ProgressChanged);

                Uri uri = new Uri (url);

                sw.Start ();

                try {
                    webClient.DownloadFileAsync (uri, localPath);
                } catch (Exception ex) {
                    Log.Error (ex);
                }
            }
        }

        // The event that will fire whenever the progress of the WebClient is changed
        private void ProgressChanged (object sender, DownloadProgressChangedEventArgs e)
        {
            string speed = string.Format ("{0} kB/s", (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString ("0.00"));

            int percentage = e.ProgressPercentage;

            int size = string.Format ("{0} MB's / {1} MB's",
                           (e.BytesReceived / 1024d / 1024d).ToString ("0.00"),
                           (e.TotalBytesToReceive / 1024d / 1024d).ToString ("0.00"));

            Console.Write (percentage);

            int left = Console.CursorLeft;
            string line = string.Format ("{0} {1:P2} {2}{3}", Description, progress, currentDescription, etaString);
            if (line.Length > MAX_WIDTH) {
                line = line.Substring (0, MAX_WIDTH);
            }
            Console.Write (line + String.Concat (Enumerable.Repeat (" ", Math.Max (0, MAX_WIDTH - line.Length))));
            Console.CursorLeft = left;
            Console.Out.Flush ();
        }

        // The event that will trigger when the WebClient is completed
        private void Completed (object sender, AsyncCompletedEventArgs e)
        {
            sw.Reset ();

            // if (e.Cancelled == true) {
        }
    }
}

