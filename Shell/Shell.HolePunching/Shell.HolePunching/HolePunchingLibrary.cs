using System;
using Shell.Common.IO;
using Shell.Common.Util;
using System.Diagnostics;
using System.Threading;

namespace Shell.HolePunching
{
    public class HolePunchingLibrary : Library
    {
        public HolePunchingLibrary ()
        {
            ConfigName = "HolePunching";
        }

        private readonly string SECTION = "Peer";

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

