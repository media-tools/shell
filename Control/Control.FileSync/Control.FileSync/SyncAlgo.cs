using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Control.Common;
using Control.Common.IO;
using Control.Common.Tasks;

namespace Control.FileSync
{
	public class SyncAlgo
	{
        public Tree Source { get; private set; }
        public Tree Destination { get; private set; }

        public SyncAlgo (Tree source, Tree destination)
        {
            Source = source;
            Destination = destination;
            Log.Message ("Synchronization:");
            Log.Indent ++;
            Log.Message ("from: ", source);
            Log.Message ("to:   ", destination);
            Log.Indent --;
        }

        public void Synchronize ()
        {
        }
	}
}

