using System;
using System.Linq;
using Mono.Options;
using Shell.Common.IO;

namespace Shell.Common.Tasks
{
    public abstract class MonoOptionsScriptTask : ScriptTask
    {
        protected Action[] Methods;

        public string[] Parameters;

        public override string[] ParameterSyntax {
            protected set {
                if (value.Length > 0 && value.Any (p => p.Length > 0)) {
                    throw new InvalidOperationException ("You have to use Parameters instead of ParameterSyntax inside a subclass of MonoOptionsScriptTask!");
                }
            }
            get {
                return Parameters.Select (p => "--" + p).ToArray ();
            }
        }

        public MonoOptionsScriptTask ()
        {
            Methods = new Action[] { };
        }

        protected override void CheckValidInternal ()
        {
            if (ParameterSyntax.Length != Methods.Length) {
                Log.Error ("Invalid parameters and methods in ", Name, ": ParameterSyntax.Length=", ParameterSyntax.Length, ", Methods.Length=", Methods.Length);
            }
        }

        protected sealed override void InternalRun (string[] args)
        {
            string command = null;
            bool help = false;

            OptionSet optionSet = new OptionSet ();

            optionSet.Add ("?|help|h",
                "Prints out the options.",
                option => help = option != null);

            optionSet.AddTaskParameters (task: this, setCommand: param => command = param);

            SetupOptions (ref optionSet);

            try {
                optionSet.Parse (args);
            } catch (OptionException) {
                printOptions (optionSet);
                return;
            }

            if (help) {
                printOptions (optionSet);
                return;
            }

            if (command != null) {
                bool foundCommand = false;
                for (int i = 0; i < Parameters.Length && i < Methods.Length; ++i) {
                    string param = Parameters [i];
                    Action method = Methods [i];

                    if (command == param) {
                        method ();
                        foundCommand = true;
                    }
                }
                if (!foundCommand) {
                    printError (optionSet);
                }
            } else {
                printError (optionSet);
            }
        }

        private void printOptions (OptionSet optionSet)
        {
            optionSet.WriteOptionDescriptions (Console.Out);
        }

        private void printError (OptionSet optionSet)
        {
            error ();
            Log.Message ();
            printOptions (optionSet);
        }

        protected abstract void SetupOptions (ref OptionSet optionSet);
    }

    public static class MonoOptionsExtensions
    {
        public static OptionSet AddTaskParameters (this OptionSet optionSet, MonoOptionsScriptTask task, Action<string> setCommand)
        {
            for (int i = 0; i < task.Parameters.Length && i < task.Description.Length; ++i) {
                string param = task.Parameters [i];
                string descr = task.Description [i];

                optionSet = optionSet.Add (param, descr, option => {
                    //Log.Debug ("aufruf: ", param);
                    setCommand (param);
                });
            }
            return optionSet;
        }
    }
}

