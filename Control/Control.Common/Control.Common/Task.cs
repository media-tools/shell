using System;
using System.Linq;

namespace Control.Common
{
    public abstract class Task
    {
        public string Name { protected set; get; }

        public string[] Options { protected set; get; }

        public string Description { protected set; get; }

        protected FileSystems fs;

        public Task ()
        {
        }

        protected void CheckValid ()
        {
            if (string.IsNullOrEmpty (Name)) {
                throw new ArgumentException ("Task(" + this + ") has illegal name: '" + Name + "'");
            }
            if (string.IsNullOrEmpty (Description)) {
                throw new ArgumentException ("Task(" + Name + ") has illegal description: '" + Description + "'");
            }
            if (Options.Length == 0) {
                throw new ArgumentException ("Task(" + Name + ") has no options!");
            }
            int i = 0;
            foreach (string option in Options) {
                if (string.IsNullOrEmpty (option)) {
                    throw new ArgumentException ("Task(" + Name + ") has illegal option #" + i + ": '" + option + "'");
                }
                ++i;
            }
        }

        public void Run (string[] args)
        {
            CheckValid ();
            
            fs = new FileSystems {
                Config = new FileSystem (task: this, type: FileSystemType.Config),
                Runtime = new FileSystem (task: this, type: FileSystemType.Runtime)
            };
            InternalRun (args);
        }

        protected abstract void InternalRun (string[] args);

        public virtual void PrintUsage (string indent)
        {
            CheckValid ();
            string line = indent + Options [0];
            line += String.Concat (Enumerable.Repeat (" ", 40 - line.Length));
            line += Description;
            Log.Message (line);
        }

        public virtual bool MatchesOption (string str)
        {
            CheckValid ();
            foreach (string option in Options) {
                if (option.ToLower () == str.ToLower ()) {
                    return true;
                }
            }
            return false;
        }

        protected struct FileSystems
        {
            public FileSystem Config;
            public FileSystem Runtime;
        }
    }
}

