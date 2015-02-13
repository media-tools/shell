using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Shell.Common.Util;

namespace Shell.Common.IO
{
    public class UserChoices
    {
        public UserChoice[] Choices { get; private set; }

        public UserChoices (IEnumerable<UserChoice> choices)
        {
            Choices = choices.ToArray ();
        }

        public void Ask (string question)
        {
            Log.Message ();
            Log.Message (LogColor.DarkGray, "Actions:", LogColor.Reset);
            Log.Indent++;
            foreach (UserChoice choice in Choices) {
                Log.Message (LogColor.DarkGray, choice, LogColor.Reset);
            }
            Log.Indent--;   
 
            do {
                Log.Message ();
                string chosen = Log.AskForString (question: question, color: LogColor.DarkGray);
                IEnumerable<UserChoice> chosenChoices = Choices.Where (c => c.Number == chosen);

                // found a direct match?
                if (chosenChoices.Any ()) {
                    Log.Message ();
                    chosenChoices.First ().Action ();
                    return;
                }
                // Ctrl-D? fuck you!
                else if (chosen == "") {
                    Commons.OnCancel ();
                    System.Environment.Exit (0);
                }
                // fuck it!
                else {
                    Log.Error ("Invalid input. Please choose one of the following options: ", string.Join (", ", Choices.Select (c => c.Number)));
                }
            } while (true);
        }
    }
}
