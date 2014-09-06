using System;
using Shell.Common.IO;
using Shell.Common.Util;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Linq;

namespace Shell.HolePunching
{
    public class HolePunchingLibrary : Library
    {
        public static int KEEP_ALIVE_TIMEOUT = 10000;
        private static byte[] KEEP_ALIVE_BYTES = Encoding.ASCII.GetBytes ("KEEP-ALIVE");
        private readonly string SECTION = "Peer";

        public HolePunchingLibrary ()
        {
            ConfigName = "HolePunching";
        }

        public static bool IsKeepAlivePacket (byte[] bytes)
        {
            return bytes.SequenceEqual (KEEP_ALIVE_BYTES);
        }

        public static void SendKeepAlivePackets (UdpClient udp, IPEndPoint udpRemote, Func<bool> checkIfRunning)
        {
            System.Threading.Tasks.Task.Run (async () => {
                while (checkIfRunning ()) {
                    await udp.SendAsync (KEEP_ALIVE_BYTES, KEEP_ALIVE_BYTES.Length, udpRemote);
                    Thread.Sleep (2000);
                }
            });
        }

        public void ReadConfig (out string peer, out int myoffset, out int peeroffset)
        {
            ConfigFile config = fs.Config.OpenConfigFile ("peer.ini");

            peer = "";
            myoffset = 0;
            peeroffset = 0;

            int i = 0;
            while (true) {
                peer = config [SECTION, "peer_hostname", ""];
                myoffset = config.GetOptionInt (SECTION, "local_portoffset", 0);
                peeroffset = config.GetOptionInt (SECTION, "peer_portoffset", 0);

                if (peer == "" || myoffset == 0 || peeroffset == 0 || myoffset == peeroffset) {
                    if (Commons.CurrentPlatform == Platforms.Linux) {
                        if (i == 0) {
                            Log.Debug ("Linux...");
                            Process.Start (@"gedit", config.Filename);
                        }
                    } else {
                        if (i == 0) {
                            Log.Debug ("Windows...");
                            Process.Start (@"notepad.exe", config.Filename);
                        }
                    }

                    Thread.Sleep (1000);
                    config.Reload ();
                    ++i;
                } else {
                    break;
                }
            }
        }
    }
}

