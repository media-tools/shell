using System;
using System.Net.Sockets;
using System.Net;
using Shell.Common.IO;
using System.Text;
using System.Threading;
using Shell.Common;
using System.Threading.Tasks;

namespace Shell.HolePunching
{
    public class NatTraverse
    {
        private static readonly string GARBAGE_MAGIC = "nat-traverse-garbage";
        private static readonly int PACKET_SIZE = 8 * 1024;

        public static int WINDOW = 10;
        public static int TIMEOUT = 30;

        public ushort LocalPort { get; private set; }

        public ushort RemotePort { get; private set; }

        public string RemoteHost { get; private set; }

        private IPEndPoint RemoteEndPoint;

        private Random rnd = new Random ();

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
                peerAddr = IPAddress.Parse (RemoteHost);
            } catch (FormatException) {
                peerAddr = Dns.GetHostAddresses (RemoteHost) [0];
            }
            RemoteEndPoint = new IPEndPoint (peerAddr, RemotePort);
            return sock;
        }


        public bool Punch ()
        {
            UdpClient sock = SockGen ();
            bool success = false;
            while (!success) {
                TryToConnect (sock: sock);
                success = HandShake (sock: sock);
            }

            if (success) {
                Log.Message ("Connection established.");
            } else {
                Log.Message ("Failed to connect.");
            }
            return success;
        }

        void TryToConnect (UdpClient sock)
        {
            bool running = true;

            Task.Run (async () => {
                while (running) {
                    //IPEndPoint object will allow us to read datagrams sent from any source.
                    UdpReceiveResult receivedResults = await sock.ReceiveAsync ();
                    string receivedString = Encoding.ASCII.GetString (receivedResults.Buffer);
                    if (receivedString.Trim ().StartsWith (GARBAGE_MAGIC)) {
                        Log.Message ("Received garbage...");
                        running = false;
                    } else {
                        Log.Debug ("Received: ", receivedString);
                    }
                }
            });

            Log.Message ("Trying to connect... ");

            byte[] garbage = Encoding.ASCII.GetBytes (GARBAGE_MAGIC);
            while (running) {
                Console.Error.Write (".");
                Console.Error.Flush ();
                try {
                    sock.SendAsync (garbage, garbage.Length, RemoteEndPoint);
                } catch (Exception ex) {
                    Log.Debug (ex.Message);
                }
                Thread.Sleep (1000);
            }
            Console.WriteLine ();
        }

        bool HandShake (UdpClient sock)
        {
            Log.Message ("Handshake...");
            int sum = SendNumbers (sock: sock);
            return VerifySum (sock: sock, sumSent: sum);

        }

        int SendNumbers (UdpClient sock)
        {
            int iterationsNeeded = 0;
            bool running = true;
            Task.Run (async () => {
                int receivedSum = 0;
                int receivedNumbers = 0;
                while (running) {
                    UdpReceiveResult receivedResults = await sock.ReceiveAsync ();
                    string receivedString = Encoding.ASCII.GetString (receivedResults.Buffer);
                    int num;
                    if (int.TryParse (receivedString.Trim (), out num)) {
                        receivedSum += num;
                        receivedNumbers++;
                        Log.Message ("Received number #", receivedNumbers, ":", num);
                        if (receivedNumbers == 3) {
                            running = false;
                        }
                    } else {
                        Log.Debug ("Received instead of number: ", receivedString);
                    }
                }

                byte[] sumBytes = Encoding.ASCII.GetBytes (receivedSum + "");
                await sock.SendAsync (sumBytes, sumBytes.Length, RemoteEndPoint);
            });

            int sum = 0;
            for (int i = 0; i < 3; ++i) {
                int num = rnd.Next (1, 9999999);
                sum += num;
                Log.Message ("Send number #", (i + 1), ":", num);
                byte[] numBytes = Encoding.ASCII.GetBytes (num + "");
                sock.SendAsync (numBytes, numBytes.Length, RemoteEndPoint);
            }

            while (running) {
                Thread.Sleep (100);
            }

            return sum;
        }

        bool VerifySum (UdpClient sock, int sumSent)
        {
            bool success = false;
            bool running = true;
            Task.Run (async () => {
                UdpReceiveResult receivedResults = await sock.ReceiveAsync ();
                string receivedString = Encoding.ASCII.GetString (receivedResults.Buffer);
                int returnedSum = 0;
                if (int.TryParse (receivedString.Trim (), out returnedSum)) {
                    Log.Message ("Received sum:", returnedSum);
                    if (returnedSum == sumSent) {
                        success = true;
                    } else {
                        success = false;
                    }
                    running = false;
                } else {
                    Log.Debug ("Received instead of sum: ", receivedString);
                }
            });
            while (running) {
                Thread.Sleep (100);
            }
            return success;
        }
    }
}

