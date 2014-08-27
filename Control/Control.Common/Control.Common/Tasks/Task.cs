using System;
using System.Linq;
using Control.Common.IO;

namespace Control.Common.Tasks
{
    public abstract class Task
    {
        public string Name { protected set; get; }

        private string _configName;

        public string ConfigName {
            get { return _configName ?? Name; }
            set { _configName = value; }
        }

        public string[] Options { protected set; get; }

        public string ParameterSyntax { protected set; get; }

        public string Description { protected set; get; }

        protected FileSystems fs;

        public Task ()
        {
            ParameterSyntax = "";
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

        public virtual int LengthOfUsageLine (string indent)
        {
            return indent.Length + Options [0].Length + 1 + ParameterSyntax.Length;
        }

        public virtual void PrintUsage (string indent, int maxLength)
        {
            CheckValid ();

            int lineLength = 0;
            Console.ResetColor ();
            Console.Write (indent);
            lineLength += indent.Length;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write (Options [0]);
            lineLength += Options [0].Length;
            if (ParameterSyntax.Length != 0) {
                Console.ResetColor ();
                Console.Write (" ");
                lineLength += 1;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write (ParameterSyntax);
                lineLength += ParameterSyntax.Length;
            }
            Console.ResetColor ();
            Console.Write (String.Concat (Enumerable.Repeat (" ", maxLength - lineLength)));
            // Console.ForegroundColor = ConsoleColor.;
            Console.WriteLine (Description);
            Console.ResetColor ();
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

        public override string ToString ()
        {
            return string.Format ("Task({0})", Name);
        }

        public override int GetHashCode ()
        {
            return ToString ().GetHashCode ();
        }

        public override bool Equals (object obj)
        {
            return Equals (obj as Task);
        }

        public bool Equals (Task obj)
        {
            return obj != null && GetHashCode () == obj.GetHashCode ();
        }
    }

    public struct FileSystems
    {
        public FileSystem Config;
        public FileSystem Runtime;
    }
}

