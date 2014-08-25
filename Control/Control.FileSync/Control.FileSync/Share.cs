using System;
using System.Collections.Generic;
using System.IO;
using Control.Common;
using Control.Common.Tasks;
using Control.Common.IO;

namespace Control.FileSync
{
    public class Share
    {
        public string Name { get; private set; }

        public HashSet<Tree> Trees { get; private set; }

        public Share (string name)
        {
            Name = name;
            Trees = new HashSet<Tree> ();
        }

        public void Add (Tree tree)
        {
            if (tree != null) {
                if (tree.Name != Name) {
                    throw new ArgumentException (string.Format ("This tree does not belong in share \"{0}\": {1}", Name, tree));
                }
                Trees.Add (tree);
            } else {
                throw new ArgumentNullException (string.Format ("Invalid Tree: {0}", tree));
            }
        }

        public override string ToString ()
        {
            return string.Format ("Share(Name=\"{0}\")", Name);
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode ();
        }
    }
}

