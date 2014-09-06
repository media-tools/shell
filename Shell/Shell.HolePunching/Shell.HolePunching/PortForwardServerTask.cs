using System;
using Shell.Common.Tasks;
using System.Net;
using System.Net.Sockets;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Util;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Shell.HolePunching
{
    public class PortForwardServerTask : ScriptTask, MainScriptTask
    {
        public PortForwardServerTask ()
        {
            Name = "HolePunching";
            Description = "Forward a port (server)";
            Options = new string[] { "hole-punching-port-forward-server", "hp-pf-server" };
            ConfigName = "HolePunching";
            ParameterSyntax = "";
        }

        protected override void InternalRun (string[] args)
        {
            string peer;
            int myoffset;
            int peeroffset;
            new HolePunchingUtil ().ReadConfig (peer: out peer, myoffset: out myoffset, peeroffset: out peeroffset);

            LocalPort local = Networking.OpenLocalPort (offset: myoffset);

            Listen (local, peer, peeroffset);
        }

        void Listen (LocalPort local, string peer, int peeroffset)
        {
            while (true) {
                UdpConnection conn = local.OpenConnection (remoteHost: peer, remoteOffset: peeroffset);
                conn.PunchHole ();

                ushort targetPort;
                if (GetTarget (connection: conn, targetPort: out targetPort)) {
                    TcpClient tcpSock = ConnectTcp (port: targetPort);
                    if (tcpSock != null) {
                        Task.Run (async () => await HolePunchingUtil.RedirectEverything (udp: conn, tcp: tcpSock));
                    } else {
                        Log.Error ("Unable to connect to tcp target.");
                    }
                } else {
                    Log.Error ("Unable to get target port.");
                }
            }
        }

        bool GetTarget (UdpConnection connection, out ushort targetPort)
        {
            bool running = true;
            bool success = false;
            int _targetPort = 0;
            int timeout = HolePunchingUtil.KEEP_ALIVE_TIMEOUT;

            CancellationTokenSource source = new CancellationTokenSource ();

            System.Threading.Tasks.Task.Run (async () => {
                while (running) {
                    Packet packet = await connection.ReceiveAsync ();
                    string receivedString = packet.BufferString;
                    if (receivedString.StartsWith ("TARGET:")) {
                        string target = receivedString.Substring (7);
                        Log.Message ("Received target: ", target);
                        if (int.TryParse (target, out _targetPort)) {
                            running = false;
                            success = true;
                            connection.Send ("TARGET OK");
                            source.Cancel ();
                        } else {
                            Log.Error ("Invalid target port: ", target);
                            running = false;
                            success = false;
                            connection.SendError ("Invalid Target: ", target);
                            source.Cancel ();
                        }
                        timeout = HolePunchingUtil.KEEP_ALIVE_TIMEOUT;

                    } else {
                        Log.Debug ("Received shit while waiting for target: ", receivedString);
                    }
                }
            }, source.Token);

            connection.SendKeepAlivePackets (() => running, source.Token);

            while (running) {
                Thread.Sleep (100);
                timeout -= 100;
                if (timeout <= 0) {
                    Log.Error ("Timeout!");
                    running = false;
                    source.Cancel ();
                }
            }
            targetPort = (ushort)_targetPort;

            return success;
        }

        TcpClient ConnectTcp (ushort port)
        {
            TcpClient tcpSock;
            try {
                tcpSock = new TcpClient ();
                tcpSock.Connect ("127.0.0.1", port);
            } catch (Exception ex) {
                Log.Error (ex);
                tcpSock = null;
            }
            return tcpSock;
        }
    }
}

