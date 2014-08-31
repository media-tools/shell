using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Control.Common.Util;

namespace Control.Common.IO
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
            Log.Message ("Actions:");
            Log.Indent++;
            foreach (UserChoice choice in Choices) {
                Log.Message (choice);
            }
            Log.Indent--;   
 
            do {
                Log.Message ();
                Console.Write (question.TrimEnd(' ')+" ");
                Console.Out.Flush ();
                string chosen = Console.ReadLine ().Trim (' ', '\r', '\n', '\t');
                Log.MessageLog ("User Input => ", chosen);
                IEnumerable<UserChoice> chosenChoices = Choices.Where (c => c.Number.ToString () == chosen);
                if (chosenChoices.Any ()) {
                    Log.Message ();
                    chosenChoices.First ().Action ();
                    return;
                } else {
                    Log.Error ("Invalid input. Please choose one of the following options: ", string.Join (", ", Choices.Select (c => c.Number)));
                }
            } while (true);
        }
    }

}
