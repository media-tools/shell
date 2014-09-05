using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Shell.Common.Util;

namespace Shell.Common.IO
{
    public static class UserChoiceExtensions
    {
        public static UserChoice[] ToUserChoices <A> (this IEnumerable<A> enumerable, Action<A> action)
        {
            List<UserChoice> choices = new List<UserChoice> ();
            int i = 1;
            foreach (A item in enumerable) {
                choices.Add (new UserChoice (number: i, name: item.ToString (), action: () => action (item)));
                i++;
            }
            return choices.ToArray ();
        }
    }
}
