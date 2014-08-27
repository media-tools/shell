using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Control.Common;
using Control.Common.IO;
using Control.Common.Tasks;

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
            return string.Format ("Share(name=\"{0}\")", Name);
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode ();
        }

        public override bool Equals (object obj)
        {
            return Equals (obj as Share);
        }

        public bool Equals (Share obj)
        {
            return obj != null && GetHashCode () == obj.GetHashCode ();
        }

        public void Synchronize ()
        {
            int pairs = 0;
            IEnumerable<Tree> readableTrees = from tree in Trees where tree.IsReadable select tree;
            IEnumerable<Tree> writableTrees = from tree in Trees where tree.IsWriteable select tree;
            foreach (Tree readableTree in readableTrees) {
                foreach (Tree writableTree in from tree in writableTrees where tree != readableTree select tree) {
                    SyncAlgo algo = new SyncAlgo (source: readableTree, destination: writableTree);
                    algo.Synchronize ();
                    pairs ++;
                }
            }

            if (pairs == 0) {
                Log.Message ("No trees are available for synchronization.");
            }
        }
    }
}

