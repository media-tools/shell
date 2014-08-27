using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Control.Common;
using Control.Common.IO;
using Control.Common.Tasks;

namespace Control.FileSync
{
    public class SyncAlgo
    {
        public Tree Source { get; private set; }

        public Tree Destination { get; private set; }

        public SyncAlgo (Tree source, Tree destination)
        {
            Source = source;
            Destination = destination;
            Log.Message ("Synchronization:");
            Log.Indent ++;
            Log.Message ("Source:      ", source);
            Log.Message ("Destination: ", destination);
            Log.Indent --;
        }

        public void Synchronize ()
        {
            Log.Indent ++;
            Source.CreateIndex ();
            Destination.CreateIndex ();

            LookForChanges ();
            Log.Indent --;
        }

        private void LookForChanges ()
        {
            Log.Message ("Changes: ");
            Log.Indent ++;
            foreach (DataFile sourceFile in Source.Files) {
                DataFile destFile;
                if (Destination.ContainsFile (search: sourceFile, result: out destFile)) {
                    if (sourceFile.ContentEquals (otherFile: destFile)) {
                        Log.Message (LogColor.DarkGray, "[unchanged] ", LogColor.Reset, sourceFile);
                    } else {
                        TimeSpan diff = sourceFile.GetWriteTimeDiff (otherFile: destFile);
                        if (diff.TotalMilliseconds > 0) {
                            Log.Message (LogColor.DarkGreen, "[newer ", diff.Verbose (), "] ", LogColor.Reset, sourceFile);
                        } else if (diff.TotalMilliseconds < 0) {
                            Log.Message (LogColor.DarkYellow, "[older ", diff.Negate ().Verbose (), "] ", LogColor.Reset, sourceFile);
                        } else {
                            Log.Message (LogColor.DarkGreen, "[changed] ", LogColor.Reset, sourceFile);
                        }
                    }
                } else {
                    Log.Message (LogColor.DarkGreen, "[created] ", LogColor.Reset, sourceFile);
                }
            }
            foreach (DataFile destFile in Destination.Files) {
                DataFile sourceFile;
                if (!Source.ContainsFile (search: destFile, result: out sourceFile)) {
                    Log.Message (LogColor.DarkRed, "[deleted] ", LogColor.Reset, destFile);
                }
            }
            Log.Indent --;
        }
    }
}

