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

        public ChangesList Changes { get; private set; }

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
            PrintChanges ();
            ApplyChanges ();
            Log.Indent --;
        }

        private void LookForChanges ()
        {
            Changes = new ChangesList ();

            foreach (DataFile sourceFile in Source.Files) {
                DataFile destFile;
                if (Destination.ContainsFile (search: sourceFile, result: out destFile)) {
                    if (sourceFile.ContentEquals (otherFile: destFile)) {
                        Changes.Unchanged.Add (sourceFile);
                    } else {
                        TimeSpan diff = sourceFile.GetWriteTimeDiff (otherFile: destFile);
                        if (diff.TotalMilliseconds > 0) {
                            Changes.Newer.Add (Tuple.Create (sourceFile, diff));
                        } else if (diff.TotalMilliseconds < 0) {
                            Changes.Older.Add (Tuple.Create (sourceFile, diff));
                        } else {
                            Changes.Changed.Add (sourceFile);
                        }
                    }
                } else {
                    Changes.Created.Add (sourceFile);
                }
            }
            foreach (DataFile destFile in Destination.Files) {
                DataFile sourceFile;
                if (!Source.ContainsFile (search: destFile, result: out sourceFile)) {
                    Changes.Deleted.Add (destFile);
                }
            }
            Log.Indent --;
        }

        private void PrintChanges ()
        {
            Log.Message ("Changes: ");
            Log.Indent ++;
            foreach (DataFile sourceFile in Changes.Unchanged) {
                Log.Message (LogColor.DarkGray, "[unchanged] ", LogColor.Reset, sourceFile);
            }
            foreach (Tuple<DataFile, TimeSpan> tuple in Changes.Older) {
                DataFile sourceFile = tuple.Item1;
                TimeSpan diff = tuple.Item2;
                Log.Message (LogColor.DarkYellow, "[older ", diff.Negate ().Verbose (), "] ", LogColor.Reset, sourceFile);
            }
            foreach (Tuple<DataFile, TimeSpan> tuple in Changes.Newer) {
                DataFile sourceFile = tuple.Item1;
                TimeSpan diff = tuple.Item2;
                Log.Message (LogColor.DarkGreen, "[newer ", diff.Verbose (), "] ", LogColor.Reset, sourceFile);
            }
            foreach (DataFile sourceFile in Changes.Changed) {
                Log.Message (LogColor.DarkGreen, "[changed] ", LogColor.Reset, sourceFile);
            }
            foreach (DataFile sourceFile in Changes.Created) {
                Log.Message (LogColor.DarkGreen, "[created] ", LogColor.Reset, sourceFile);
            }
            foreach (DataFile destFile in Changes.Deleted) {
                Log.Message (LogColor.DarkRed, "[deleted] ", LogColor.Reset, destFile);
            }
            Log.Indent --;
        }

        private void ApplyChanges ()
        {

        }

        public class ChangesList
        {
            public List<DataFile> Unchanged = new List<DataFile> ();
            public List<Tuple<DataFile, TimeSpan>> Newer = new List<Tuple<DataFile, TimeSpan>> ();
            public List<Tuple<DataFile, TimeSpan>> Older = new List<Tuple<DataFile, TimeSpan>> ();
            public List<DataFile> Changed = new List<DataFile> ();
            public List<DataFile> Created = new List<DataFile> ();
            public List<DataFile> Deleted = new List<DataFile> ();
        }
    }
}

