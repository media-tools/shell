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
        private static readonly string ACK_MAGIC = "nat-traverse-ackacka";

        public static int WINDOW = 10;
        public static int TIMEOUT = 10;

        public UdpConnection Connection { get; private set; }

        public NatTraverse (UdpConnection connection)
        {
            Connection = connection;
        }

        public bool Punch ()
        {
            bool success = false;
            while (!success) {
                TryToConnect ();
                success = true;
            }

            if (success) {
                Log.Message ("Connection established.");
            } else {
                Log.Message ("Failed to connect.");
            }
            return success;
        }

        void TryToConnect ()
        {
            bool sendingGarbage = true;
            bool waitingForAck = false;

            Task.Run (async () => {
                while (sendingGarbage || waitingForAck) {
                    Packet packet = await Connection.ReceiveAsync ();
                    string receivedString = Encoding.ASCII.GetString (packet.Buffer);
                    if (receivedString.Trim ().StartsWith (GARBAGE_MAGIC)) {
                        Log.Message ("Received garbage...");
                        sendingGarbage = false;
                        waitingForAck = true;
                        Log.Message ("Sending ack...");
                        Connection.Send (ACK_MAGIC);
                    } else if (receivedString.Trim ().StartsWith (ACK_MAGIC)) {
                        Log.Message ("Received ack...");
                        Connection.Send (ACK_MAGIC);
                        sendingGarbage = false;
                        waitingForAck = false;
                    } else {
                        Log.Debug ("Received: ", receivedString);
                    }
                }
            });

            Connection.SendKeepAlivePackets (whileTrue: () => sendingGarbage);

            Log.Message ("Sending garbage... ");
            while (sendingGarbage) {
                Connection.Send (GARBAGE_MAGIC);
                Thread.Sleep (500);
            }

            Log.Message ("Waiting for ack... ");
            while (waitingForAck) {
                Connection.Send (ACK_MAGIC);
                Thread.Sleep (500);
            }

            Log.Message ("Received valid ack.");

            Thread.Sleep (2000);
        }

        /*
        bool HandShake (UdpClient sock)
        {
            Log.Message ("Handshake...");
            return SendNumbers (sock: sock);
        }

        bool SendNumbers (UdpClient sock)
        {
            bool success = true;
            int iterationsNeeded = 10;
            bool master = LocalPort > RemotePort;
            bool running = true;

            int startNumber;
            if (master) {
                Log.Message ("Master.");
                startNumber = rnd.Next (1, 9999999);
                Log.Debug ("Number (start): " + startNumber);
                byte[] startNumberBytes = Encoding.ASCII.GetBytes (startNumber + "");
                sock.Send (startNumberBytes, startNumberBytes.Length, RemoteEndPoint);
            }

            Task.Run (async () => {
                int lastNumber = -1;
                while (running) {
                    UdpReceiveResult receivedResults = await sock.ReceiveAsync ();
                    string receivedString = Encoding.ASCII.GetString (receivedResults.Buffer);
                    int num;
                    if (int.TryParse (receivedString.Trim (), out num)) {
                        Log.Debug ("Received number:", num);
                        if (lastNumber == -1) {
                            startNumber = num;
                        } else if (lastNumber != -1 && num - 2 != lastNumber) {
                            Log.Error ("Received wrong number: ", num);
                            success = false;
                            running = false;
                        } else if (lastNumber - startNumber > iterationsNeeded) {
                            Log.Message ("Enough.");
                            running = false;
                        }

                        Log.Debug ("Send number:", num + 1);
                        byte[] nextNumberBytes = Encoding.ASCII.GetBytes ((num + 1) + "");
                        await sock.SendAsync (nextNumberBytes, nextNumberBytes.Length, RemoteEndPoint);
                        lastNumber = num;
                    } else {
                        Log.Debug ("Received instead of number: ", receivedString);
                    }
                }
            });

            int time = 0;
            while (running) {
                Thread.Sleep (100);
                time += 100;
                if (time > TIMEOUT * 1000) {
                    success = false;
                    running = false;
                }
            }

            return success;
        }*/
    }
}

