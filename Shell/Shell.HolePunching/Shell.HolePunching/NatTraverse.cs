using System;
using System.Net.Sockets;
using System.Net;
using Shell.Common.IO;
using System.Text;
using System.Threading;
using Shell.Common;

namespace Shell.HolePunching
{
    public class NatTraverse
    {
        private static readonly string GARBAGE_MAGIC = "nat-traverse-garbage";
        private static readonly string ACK_MAGIC = "nat-traverse-ackacka";
        private static readonly int PACKET_SIZE = 8 * 1024;

        public static int WINDOW = 10;
        public static int TIMEOUT = 30;

        public ushort LocalPort { get; private set; }

        public ushort RemotePort { get; private set; }

        public string RemoteHost { get; private set; }

        private IPEndPoint RemoteEndPoint;

        public NatTraverse (ushort localPort, string remoteHost, ushort remotePort)
        {
            LocalPort = localPort;
            RemoteHost = remoteHost;
            RemotePort = remotePort;
        }

        private UdpClient SockGen ()
        {
            Log.Message ("Creating socket localhost:", LocalPort, " <-> ", RemoteHost, ":", RemotePort, "...");
            UdpClient sock = new UdpClient ();
            sock.ExclusiveAddressUse = false;
            sock.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            IPEndPoint localpt = new IPEndPoint (IPAddress.Any, LocalPort);
            sock.Client.Bind (localpt);
            IPAddress peerAddr;
            try {
                peerAddr = IPAddress.Parse(RemoteHost);
            }catch (FormatException) {
                peerAddr = Dns.GetHostAddresses(RemoteHost)[0];
            }
            RemoteEndPoint = new IPEndPoint (peerAddr, RemotePort);
            return sock;
        }

        private void WaitFor (UdpClient sock, string match)
        {
            IPEndPoint remote = new IPEndPoint (IPAddress.Any, 0);

            while (true) {
                try {
                    Console.Error.Write (".");
                    Console.Error.Flush ();
                    byte[] data = sock.Receive (ref remote);
                    if (Encoding.ASCII.GetString (data).Trim ().StartsWith (match)) {
                        break;
                    }
                } catch (Exception ex) {
                    Log.Error (ex);
                    break;
                }
                //my $got;
                //defined(sysread $sock, $got, length $match) or
                //die "Couldn't read from socket: $!\n";
                //last if defined $got and $got eq $match;
            }
            Console.Error.WriteLine ();
            Console.Error.Flush ();
        }

        public bool Punch ()
        {
            UdpClient sock = SockGen ();
            Log.Message ("Sending " + WINDOW + " initial packets... ");

            byte[] garbage = Encoding.ASCII.GetBytes (GARBAGE_MAGIC);
            for (int i = 0; i < WINDOW; i++) {
                Console.Error.Write (".");
                Console.Error.Flush ();
                try {
                    sock.Send (garbage, garbage.Length, RemoteEndPoint);
                } catch (Exception ex) {
                    Log.Debug (ex.Message);
                }
                Thread.Sleep (1000);
            }
            Console.WriteLine ();

            Console.Error.Write ("Sending ACK...");
            Console.Error.Flush ();
            byte[] ack = Encoding.ASCII.GetBytes (ACK_MAGIC);
            sock.Send (garbage, garbage.Length, RemoteEndPoint);
            sock.Send (ack, ack.Length, RemoteEndPoint);
            Console.Error.Write ("done.");
            Console.Error.WriteLine ();

            Log.Message ("Waiting for ACK (timeout: ", TIMEOUT, ")...");
            Action waitForAck = () => {
                WaitFor (sock, ACK_MAGIC);
            };
            bool success = ThreadingUtils.CallWithTimeout (waitForAck, TIMEOUT*1000);

            if (success) {
                Log.Message ("Connection established.");
            } else {
                Log.Message ("Failed to connect.");
            }
            return success;
        }
    }
}

