using System;
using System.Linq;
using Shell.Common.IO;

namespace Shell.Common.Tasks
{
    public abstract class ScriptTask : IConfigurable
    {
        public string Name { protected set; get; }

        private string _configName;

        public string ConfigName {
            get { return _configName ?? Name; }
            set { _configName = value; }
        }

        public string[] Options { protected set; get; }

        public virtual string[] ParameterSyntax { protected set; get; }

        public string[] Description { protected set; get; }

        protected FileSystems fs;

        public ScriptTask ()
        {
            ParameterSyntax = new [] { "" };
            Description = new [] { "" };
        }

        protected void CheckValid ()
        {
            if (string.IsNullOrEmpty (Name)) {
                throw new ArgumentException ("Task(" + this + ") has illegal name: '" + Name + "'");
            }
            if (Description.Length == 0 || Description.Where (d => string.IsNullOrEmpty (d)).Any ()) {
                throw new ArgumentException ("Task(" + Name + ") has illegal description: '" + Description + "'");
            }
            if (ParameterSyntax.Length != Description.Length) {
                Log.Error ("Invalid parameters and descriptions in ", Name, ": ParameterSyntax.Length=", ParameterSyntax.Length, ", Description.Length=", Description.Length);
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
            CheckValidInternal ();
        }

        protected virtual void CheckValidInternal ()
        {
        }

        public void Run (string[] args)
        {
            CheckValid ();
            
            fs = new FileSystems {
                Config = new FileSystem (configurable: this, type: FileSystemType.Config),
                Runtime = new FileSystem (configurable: this, type: FileSystemType.Runtime)
            };
            InternalRun (args);
        }

        protected abstract void InternalRun (string[] args);

        protected void error ()
        {
            Log.Error ("One of the following options is required: " + string.Join (" | ", ParameterSyntax));
        }

        public virtual int LengthOfUsageLine (string indent, int maxOptionLength)
        {
            return indent.Length + maxOptionLength + 1 + ParameterSyntax.Max (x => x.Length) + 2;
        }

        public virtual int LengthOfOption (string indent)
        {
            return indent.Length + Options [0].Length;
        }

        public virtual void PrintUsage (string indent, int maxLineLength, int maxOptionLength)
        {
            CheckValid ();

            int maxParameterLength = ParameterSyntax.Max (x => x.Length);
            for (int p = 0; p < ParameterSyntax.Length; ++p) {
                string parameter = ParameterSyntax [p];
                string description = Description [p];

                int lineLength = 0;
                Console.ResetColor ();
                Console.Write (indent);
                lineLength += indent.Length;
                Console.ForegroundColor = ConsoleColor.DarkGreen;

                if (p == 0)
                    Console.Write (Options [0]);
                else
                    Console.Write (String.Concat (Enumerable.Repeat (" ", Options [0].Length)));

                Console.Write (String.Concat (Enumerable.Repeat (" ", maxOptionLength - Options [0].Length)));
                lineLength += maxOptionLength;

                if (ParameterSyntax.Length != 0) {
                    Console.ResetColor ();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write (parameter);
                    Console.Write (String.Concat (Enumerable.Repeat (" ", maxParameterLength - parameter.Length)));
                    lineLength += maxParameterLength;
                }
                Console.ResetColor ();
                Console.Write (String.Concat (Enumerable.Repeat (" ", maxLineLength - lineLength)));
                // Console.ForegroundColor = ConsoleColor.;
                Console.WriteLine (description);
                Console.ResetColor ();
            }
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
            return Equals (obj as ScriptTask);
        }

        public bool Equals (ScriptTask obj)
        {
            return obj != null && GetHashCode () == obj.GetHashCode ();
        }
    }

    public class FileSystems
    {
        public FileSystem Config;
        public FileSystem Runtime;
    }
}

