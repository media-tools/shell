using System;
using Shell.Common;
using Shell.Common.IO;

namespace Shell.Common
{
    public static class NetworkUtils
    {
        public static ushort CurrentPort (int offset)
        {
            ushort min = 20000;
            ushort diff = 40000;
            ushort step = 37777;
            long unix = (long)(DateTime.UtcNow.StartOfDay ().ToUnixTimestamp () * step / (24 * 60 * 60));
            Log.Debug ("unix factor: ", unix);
            ushort basePort = (ushort)(min + (ushort)(unix % diff));
            Log.Debug ("base port: ", basePort);
            ushort offsetPort = (ushort)(basePort + offset);
            Log.Debug ("port with offset ", offset, ": ", offsetPort);
            return offsetPort;
        }

    }
}

