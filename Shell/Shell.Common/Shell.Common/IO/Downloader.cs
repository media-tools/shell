using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace Shell.Common.IO
{
    public class Downloader
    {
        Stopwatch sw = new Stopwatch ();
        bool success;
        AutoResetEvent notifier;
        int lastProgress;
        double lastBytesReceived;

        public Downloader ()
        {
        }

        public bool DownloadFile (string localPath, string url)
        {
            try {
                success = true;
                using (WebClient webClient = new WebClient ()) {
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler (Completed);
                    webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler (ProgressChanged);

                    Uri uri = new Uri (url);
                    lastProgress = -1;

                    sw.Start ();

                    notifier = new AutoResetEvent (false);
                    string tempPath = localPath + ".tmp";
                    webClient.DownloadFileAsync (uri, tempPath);
                    notifier.WaitOne ();
                    File.Move (tempPath, localPath);
                }
            } catch (Exception ex) {
                Log.Message ();
                Log.Debug ("Error: Download: ", localPath, " <= ", url);
                Log.Error (ex);
                success = false;
            }
            sw.Reset ();
            return success;
        }

        public bool SetTimestamp (string localPath, DateTime timestamp)
        {
            bool success;
            try {
                if (File.Exists (localPath)) {
                    File.SetCreationTimeUtc (localPath, timestamp);
                    File.SetLastWriteTimeUtc (localPath, timestamp);
                } else {
                    Log.Error ("Error: SetTimestamp: File does not exist: ", localPath);
                }
                success = true;
            } catch (Exception ex) {
                Log.Error (ex);
                success = false;
            }
            return success;
        }

        // The event that will fire whenever the progress of the WebClient is changed
        private void ProgressChanged (object sender, DownloadProgressChangedEventArgs e)
        {
            int percentage = e.ProgressPercentage;
            double bytesReceived = e.BytesReceived;

            if (percentage > lastProgress || bytesReceived > lastBytesReceived + 100000) {
                lastProgress = percentage;
                lastBytesReceived = bytesReceived;

                string speed = string.Format ("{0} kB/s", (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString ("0.00"));

                string size = string.Format ("{0} MB's / {1} MB's",
                                  (e.BytesReceived / 1024d / 1024d).ToString ("0.00"),
                                  (e.TotalBytesToReceive / 1024d / 1024d).ToString ("0.00"));

                int left = Console.CursorLeft;
                Console.Write (String.Concat (Enumerable.Repeat (" ", Log.MAX_WIDTH)));
                Console.CursorLeft = left;
                Console.Write ("Download: " + percentage + "%    " + speed + "    " + size);
                Console.CursorLeft = left;
                Console.Out.Flush ();
            }
        }

        // The event that will trigger when the WebClient is completed
        private void Completed (object sender, AsyncCompletedEventArgs e)
        {
            Finish ();

            if (e.Cancelled == true) {
                success = false;
                Log.Error ("Download: Cancelled!");
            }

            notifier.Set ();
        }

        private void Finish ()
        {
            sw.Reset ();
            lastProgress = -1;

            int left = Console.CursorLeft;
            Console.Write (String.Concat (Enumerable.Repeat (" ", Log.MAX_WIDTH)));
            Console.CursorLeft = left;
            Console.Out.Flush ();
        }
    }
}

