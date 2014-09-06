using System;
using Shell.Common.Tasks;
using System.Net;
using System.Net.Sockets;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Util;
using System.Diagnostics;
using System.Threading;

namespace Shell.HolePunching
{
    public class ClientTask : Task, MainTask
    {
        public ClientTask ()
        {
            Name = "HolePunching";
            Description = "Run the hole punching test";
            Options = new string[] { "hole-punching-test", "hp-test" };
            ConfigName = "HolePunching";
            ParameterSyntax = "";
        }

        private readonly string SECTION = "Peer";

        protected override void InternalRun (string[] args)
        {
            string peer;
            int myoffset;
            int peeroffset;
            ReadConfig (peer: out peer, myoffset: out myoffset, peeroffset: out peeroffset);

            ushort myport = NetworkUtils.CurrentPort (myoffset);
            ushort peerport = NetworkUtils.CurrentPort (peeroffset);

            NatTraverse nattra = new NatTraverse (localPort: myport, remoteHost: peer, remotePort: peerport);
            nattra.Punch ();
        }

        private void ReadConfig (out string peer, out int myoffset, out int peeroffset)
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

