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
    public class ConnectTask : Task, MainTask
    {
        public ConnectTask ()
        {
            Name = "HolePunching";
            Description = "Run the hole punching connect test";
            Options = new string[] { "hole-punching-connect", "hp-connect" };
            ConfigName = "HolePunching";
            ParameterSyntax = "";
        }

        private readonly string SECTION = "Peer";

        protected override void InternalRun (string[] args)
        {
            string peer;
            int myoffset;
            int peeroffset;
            new HolePunchingLibrary().ReadConfig (peer: out peer, myoffset: out myoffset, peeroffset: out peeroffset);

            ushort myport = NetworkUtils.CurrentPort (myoffset);
            ushort peerport = NetworkUtils.CurrentPort (peeroffset);

            NatTraverse nattra = new NatTraverse (localPort: myport, remoteHost: peer, remotePort: peerport);
            UdpClient sock;
            nattra.Punch (out sock);
        }
    }
}

