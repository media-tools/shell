using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Tasks;

namespace Shell.FileSync
{
    public class SyncAlgo
    {
        public Tree Source { get; private set; }

        public Tree Destination { get; private set; }

        public ChangesList Changes { get; private set; }

        public SyncAlgo (Tree source, Tree destination)
        {
            Source = source;
            Source.IsSource = true;
            Destination = destination;
            Destination.IsDestination = true;
        }

        public void Synchronize ()
        {
            PrintInfo ();
            Log.Indent++;
            Source.CreateIndex ();
            Destination.CreateIndex ();

            LookForChanges ();
            PrintChanges ();
            ApplyChanges ();
            Log.Indent--;
        }

        private void PrintInfo ()
        {
            Log.Message ("Synchronization:");
            Log.Indent++;
            Log.Message ("Source:      ", Source);
            Log.Message ("Destination: ", Destination);
            Log.Indent--;
        }

        private void LookForChanges ()
        {
            ProgressBar progressBar = Log.OpenProgressBar (
                                          identifier: "FileSync:SyncAlgo:LookForChanges:" + Source.RootDirectory + ":" + Destination.RootDirectory,
                                          description: "Look for changes..."
                                      );
            float max = Source.Files.Count () + Destination.Files.Count ();
            float current = 0;

            Changes = new ChangesList (source: Source, destination: Destination);
            foreach (DataFile sourceFile in Source.Files) {

                progressBar.Print (current: current++, min: 0, max: max, currentDescription: sourceFile.Name, showETA: true, updateETA: true);

                DataFile destFile;
                if (Destination.ContainsFile (search: sourceFile, result: out destFile)) {
                    try {
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
                    } catch (Exception ex) {
                        Changes.Error.Add (Tuple.Create (sourceFile, ex));
                        //Log.Error ("Error while comparing files: ", ex.Message);
                    }
                } else {
                    Changes.Created.Add (sourceFile);
                }
            }
            foreach (DataFile destFile in Destination.Files) {

                progressBar.Print (current: current++, min: 0, max: max, currentDescription: destFile.Name, showETA: true, updateETA: true);

                DataFile sourceFile;
                
                if (!Source.ContainsFile (search: destFile, result: out sourceFile)) {
                    if (!Destination.IsSource && Destination.IsDeletable) {
                        Changes.Deleted.Add (destFile);
                    } else {
                        Changes.Inexistant.Add (destFile);
                    }
                }
            }
        }

        private void PrintChanges ()
        {
            Log.Message ("Changes: ");
            Log.Indent++;
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
            foreach (DataFile destFile in Changes.Inexistant) {
                Log.Message (LogColor.DarkGray, "[inexistant] ", LogColor.Reset, destFile);
            }
            foreach (Tuple<DataFile, Exception> tuple in Changes.Error) {
                DataFile sourceFile = tuple.Item1;
                Exception ex = tuple.Item2;
                Log.Message (LogColor.DarkGray, "[error] ", LogColor.Reset, sourceFile, " (", LogColor.Red, ex.Message, LogColor.Reset, ")");
            }
            Log.Indent--;
        }

        private void ApplyChanges ()
        {
            Log.Message ("Apply changes: ");
            Log.Indent++;
            int done = 0;
            if (Destination.IsWriteable) {
                foreach (DataFile sourceFile in Changes.Created) {
                    Log.Message (LogColor.DarkGreen, "[create] ", LogColor.Reset, sourceFile);
                    CopyFileExactly (sourceFile: sourceFile, destinationTree: Destination);
                }
                foreach (DataFile sourceFile in Changes.Changed) {
                    Log.Message (LogColor.DarkGreen, "[overwrite] ", LogColor.Reset, sourceFile);
                    CopyFileExactly (sourceFile: sourceFile, destinationTree: Destination);
                }
                foreach (DataFile sourceFile in from tuple in Changes.Newer select tuple.Item1) {
                    Log.Message (LogColor.DarkGreen, "[overwrite with newer version] ", LogColor.Reset, sourceFile);
                    CopyFileExactly (sourceFile: sourceFile, destinationTree: Destination);
                }
            }
            if (!Destination.IsSource && Destination.IsDeletable) {
                foreach (DataFile sourceFile in from tuple in Changes.Older select tuple.Item1) {
                    Log.Message (LogColor.DarkYellow, "[overwrite with older version] ", LogColor.Reset, sourceFile);
                    CopyFileExactly (sourceFile: sourceFile, destinationTree: Destination);
                }
                foreach (DataFile destFile in Changes.Deleted) {
                    Log.Message (LogColor.DarkRed, "[delete] ", LogColor.Reset, destFile);
                    DeleteFile (path: destFile);
                }
            }
            foreach (DataFile destFile in Changes.Inexistant) {
                Log.Message (LogColor.DarkGray, "[don't delete] ", LogColor.Reset, destFile);
            }

            if (done == 0) {
                Log.Message ("Nothing to do.");
            }
            Log.Indent--;
        }

        private void CopyFileExactly (DataFile sourceFile, Tree destinationTree)
        {
            CopyFileExactly (sourcePath: sourceFile.FullPath, destinationPath: Path.Combine (destinationTree.RootDirectory, sourceFile.RelativePath));
        }

        private void CopyFileExactly (string sourcePath, string destinationPath)
        {
            FileInfo source = new FileInfo (sourcePath);

            Directory.CreateDirectory (Path.GetDirectoryName (destinationPath));
            source.CopyTo (destinationPath, true);

            FileInfo destination = new FileInfo (destinationPath);
            destination.CreationTime = source.CreationTime;
            destination.LastWriteTime = source.LastWriteTime;
            destination.LastAccessTime = source.LastAccessTime;
        }

        private void DeleteFile (DataFile path)
        {
            File.Delete (path.FullPath);
        }

        public class ChangesList
        {
            public Tree Source;
            public Tree Destination;
            public List<DataFile> Unchanged = new List<DataFile> ();
            public List<Tuple<DataFile, TimeSpan>> Newer = new List<Tuple<DataFile, TimeSpan>> ();
            public List<Tuple<DataFile, TimeSpan>> Older = new List<Tuple<DataFile, TimeSpan>> ();
            public List<DataFile> Changed = new List<DataFile> ();
            public List<DataFile> Created = new List<DataFile> ();
            public List<DataFile> Deleted = new List<DataFile> ();
            public List<DataFile> Inexistant = new List<DataFile> ();
            public List<Tuple<DataFile, Exception>> Error = new List<Tuple<DataFile, Exception>> ();

            public ChangesList (Tree source, Tree destination)
            {
                Source = source;
                Destination = destination;
            }
        }
    }
}

