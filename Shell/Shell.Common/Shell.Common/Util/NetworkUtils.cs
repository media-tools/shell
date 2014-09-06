using System;
using Shell.Common;
using Shell.Common.IO;
using System.Net.Sockets;

namespace Shell.Common.Util
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

        public static bool IsStillConnected (this TcpClient tcp)
        {
            if (tcp.Client.Poll (0, SelectMode.SelectRead)) {
                byte[] buff = new byte[1];
                if (tcp.Client.Receive (buff, SocketFlags.Peek) == 0) {
                    // Client disconnected
                    return false;
                }
            }
            return true;
        }
    }
}

