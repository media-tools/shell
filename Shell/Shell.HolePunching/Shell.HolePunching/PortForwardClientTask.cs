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
    public class PortForwardClientTask : ScriptTask, MainScriptTask
    {
        public PortForwardClientTask ()
        {
            Name = "HolePunching";
            Description = "Forward a port (client)";
            Options = new string[] { "hole-punching-port-forward-client", "hp-pf-client" };
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

            List<ForwardedTcpPort> ports = new List<ForwardedTcpPort> ();
            ports.Add (new ForwardedTcpPort { Source = 10001, Target = 80 });

            Listen (forwardedPorts: ports, local: local, remoteHost: peer, remoteOffset: peeroffset);
        }

        void Listen (List<ForwardedTcpPort> forwardedPorts, LocalPort local, string remoteHost, int remoteOffset)
        {
            List<Task> tasks = new List<Task> ();
            foreach (ForwardedTcpPort forwardedPort in forwardedPorts) {
                tasks.Add (Task.Run (() => Listen (forwardedPort: forwardedPort, local: local, remoteHost: remoteHost, remoteOffset: remoteOffset)));
            }
            while (true) {
                Thread.Sleep (1000);
            }
        }

        async Task Listen (ForwardedTcpPort forwardedPort, LocalPort local, string remoteHost, int remoteOffset)
        {
            Log.Message ("Starting TCP Server: ", forwardedPort);
            TcpListener tcpServer = new TcpListener (IPAddress.Any, forwardedPort.Source);
            tcpServer.ExclusiveAddressUse = false;
            try {
                tcpServer.Start ();
                while (true) {
                    TcpClient tcpClient = await tcpServer.AcceptTcpClientAsync ();
                    Log.Message ("Connected: ", tcpClient);
                    ProcessClient (tcp: tcpClient, forwardedPort: forwardedPort, local: local, remoteHost: remoteHost, remoteOffset: remoteOffset);
                }
            } catch (Exception ex) {
                Log.Error (ex);
            } finally {
                tcpServer.Stop ();
            }
            Log.Message ("Stopped TCP Server: ", forwardedPort);
        }

        void ProcessClient (TcpClient tcp, ForwardedTcpPort forwardedPort, LocalPort local, string remoteHost, int remoteOffset)
        {
            UdpConnection conn = local.OpenConnection (remoteHost: remoteHost, remoteOffset: remoteOffset);
            conn.PunchHole ();

            if (SendTarget (connection: conn, targetPort: forwardedPort.Target)) {
                Task.Run (async () => await HolePunchingUtil.RedirectEverything (udp: conn, tcp: tcp));
            } else {
                Log.Error ("Unable to send target port.");
            }
        }

        bool SendTarget (UdpConnection connection, ushort targetPort)
        {
            bool running = true;
            bool success = false;
            int _targetPort = 0;
            int timeout = HolePunchingUtil.KEEP_ALIVE_TIMEOUT_MS;

            CancellationTokenSource source = new CancellationTokenSource ();

            connection.Send ("TARGET:" + targetPort + "");

            System.Threading.Tasks.Task.Run (async () => {
                while (running) {
                    Packet packet = await connection.ReceiveAsync ();
                    string receivedString = packet.BufferString;
                    if (receivedString.Contains ("TARGET OK")) {
                        Log.Message ("Received ack for target!");
                        running = false;
                        success = true;
                        timeout = HolePunchingUtil.KEEP_ALIVE_TIMEOUT_MS;
                        source.Cancel ();
                    } else if (packet.IsErrorPacket) {
                        Log.Error ("Received error packet: ", packet.ErrorString);
                        running = false;
                        success = false;
                        timeout = HolePunchingUtil.KEEP_ALIVE_TIMEOUT_MS;
                        source.Cancel ();
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
    }

    public struct ForwardedTcpPort
    {
        public ushort Source;
        public ushort Target;

        public override string ToString ()
        {
            return string.Format ("ForwardedPort: {0} -> {1}", Source, Target);
        }
    }
}

