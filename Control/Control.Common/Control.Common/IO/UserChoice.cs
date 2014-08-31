using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Control.Common.Util;

namespace Control.Common.IO
{
    public class UserChoice
    {
        public int Number { get; private set; }

        public string Name { get; private set; }

        public Action Action { get; private set; }

        public UserChoice (int number, string name, Action action)
        {
            Number = number;
            Name = name;
            Action = action;
        }

        public override string ToString ()
        {
            return string.Format ("{0}. {1}", Number.ToString ().PadLeft (2), Name);
        }
    }

}
