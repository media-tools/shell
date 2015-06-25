using System;
using System.Linq;
using Mono.Options;
using Shell.Common.IO;
using Shell.Common.Util;
using System.Collections.Generic;

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

        protected virtual void HookBeforeOptionParsing ()
        {
        }

        protected sealed override void InternalRun (string[] args)
        {
            HookBeforeOptionParsing ();

            string command = null;
            bool help = false;

            OptionSet optionSet = CreateOptionSet (setCommand: param => command = param, setHelp: b => help = b);

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
            Log.Info ();
            printOptions (optionSet);
        }

        private OptionSet CreateOptionSet (Action<string> setCommand, Action<bool> setHelp)
        {
            OptionSet optionSet = new OptionSet ();

            optionSet.Add ("h|help|?", "Prints out the options.", option => setHelp (option != null));
            optionSet.Add ("d|debug", "Show debugging messages.", option => Log.DEBUG_ENABLED = option != null);
            optionSet.Add ("<>", option => {
                Log.Error ("Invalid parameter: ", option);
                Log.Info ();
                setHelp (true);
            });

            optionSet.AddTaskParameters (task: this, setCommand: setCommand);

            SetupOptions (ref optionSet);

            return optionSet;
        }

        public string[] OptionSetParameters {
            get {
                OptionSet optionSet = CreateOptionSet (setCommand: Actions.Empty, setHelp: Actions.Empty);
                List<string> parameters = new List<string> ();

                foreach (Option option in optionSet) {
                    foreach (string name in option.GetNames()) {
                        if (name == "<>") {
                            // ignore
                        } else if (name.Length == 1) {
                            parameters.Add ("-" + name);
                        } else {
                            parameters.Add ("--" + name);
                        }
                    }
                }
                return parameters.ToArray ();
            }
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

