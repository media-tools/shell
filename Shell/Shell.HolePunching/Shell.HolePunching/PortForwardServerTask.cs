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

namespace Shell.HolePunching
{
    public class PortForwardServerTask : Task, MainTask
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

            UdpConnection conn = Networking.OpenLocalPort (offset: myoffset).OpenConnection (remoteHost: peer, remoteOffset: peeroffset);
            conn.PunchHole ();

            UdpClient sock = conn.Local.Socket;
            IPEndPoint remote = conn.RemoteEndPoint;

            ushort targetPort;
            if (GetTarget (sock: sock, udpRemote: remote, targetPort: out targetPort)) {
                TcpClient tcpSock = ConnectTcp (port: targetPort);
                if (tcpSock != null) {
                    ForwardPort (udp: sock, udpRemote: remote, tcp: tcpSock);
                } else {
                    Log.Error ("Unable to connect to tcp target.");
                }
            } else {
                Log.Error ("Unable to get target port.");
            }
        }

        bool GetTarget (UdpClient sock, IPEndPoint udpRemote, out ushort targetPort)
        {
            bool running = true;
            bool success = false;
            int _targetPort = 0;
            int timeout = HolePunchingUtil.KEEP_ALIVE_TIMEOUT;

            System.Threading.Tasks.Task.Run (async () => {
                while (running) {
                    UdpReceiveResult receivedResults = await sock.ReceiveAsync ();
                    string receivedString = Encoding.ASCII.GetString (receivedResults.Buffer).Trim ();
                    if (receivedString.StartsWith ("TARGET:")) {
                        string target = receivedString.Substring (7);
                        Log.Message ("Received target: ", target);
                        if (int.TryParse (target, out _targetPort)) {
                            running = false;
                            success = true;
                        } else {
                            Log.Error ("Invalid target port: ", target);
                            running = false;
                            success = false;
                        }
                        timeout = HolePunchingUtil.KEEP_ALIVE_TIMEOUT;

                    } else {
                        Log.Debug ("Received shit while waiting for target: ", receivedString);
                    }
                }
            });

            HolePunchingUtil.SendKeepAlivePackets (udp: sock, udpRemote: udpRemote, checkIfRunning: () => running);

            while (running) {
                Thread.Sleep (100);
                timeout -= 100;
                if (timeout <= 0) {
                    Log.Error ("Timeout!");
                    running = false;
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

        void ForwardPort (UdpClient udp, IPEndPoint udpRemote, TcpClient tcp)
        {
            bool running = true;

            System.Threading.Tasks.Task.Run (async () => {
                while (running) {
                    UdpReceiveResult receivedResults = await udp.ReceiveAsync ();
                    if (HolePunchingUtil.IsKeepAlivePacket (receivedResults.Buffer)) {
                        Log.Debug ("Received Keep-Alive Packet");
                    } else {
                        await tcp.GetStream ().WriteAsync (buffer: receivedResults.Buffer, offset: 0, count: receivedResults.Buffer.Length);
                        Log.Debug ("Forward (udp -> tcp): ", receivedResults.Buffer.Length, " bytes");
                    }
                }
            });

            System.Threading.Tasks.Task.Run (async () => {
                byte[] buffer = new byte[8 * 1024];

                while (running) {
                    int bytesRead = await tcp.GetStream ().ReadAsync (buffer, 0, (int)buffer.Length);
                    await udp.SendAsync (buffer, bytesRead, udpRemote);
                }
            });

            HolePunchingUtil.SendKeepAlivePackets (udp: udp, udpRemote: udpRemote, checkIfRunning: () => running);

            while (running) {
                Thread.Sleep (1000);
            }
        }
    }
}

