using System;
using Shell.Common.IO;
using System.Collections.Generic;
using Shell.Common;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Collections;
using System.Linq;
using Shell.Common.Util;
using System.Threading;

namespace Shell.HolePunching
{
    public class Networking : Library
    {
        private Networking ()
        {
            ConfigName = "HolePunching";
        }

        private static Dictionary<int, LocalPort> localPorts = new Dictionary<int, LocalPort> ();

        public static LocalPort OpenLocalPort (int offset)
        {
            if (localPorts.ContainsKey (offset)) {
                return localPorts [offset];
            }
            return localPorts [offset] = new LocalPort (offset: offset);
        }

    }

    public class LocalPort
    {
        public int Offset { get; private set; }

        public ushort Port { get { return NetworkUtils.CurrentPort (Offset); } }

        public UdpClient Socket;

        private Dictionary<ConnectionID, UdpConnection> KnownConnections = new Dictionary<ConnectionID, UdpConnection> ();
        private Dictionary<ConnectionID, Queue<Packet>> Queues = new Dictionary<ConnectionID, Queue<Packet>> ();

        public LocalPort (int offset)
        {
            Offset = offset;
            Socket = CreateSocket ();
            Queues [ConnectionID.Unknown] = new Queue<Packet> ();

            Task.Run (async () => await ProcessIO ());
        }

        public UdpConnection OpenConnection (string remoteHost, int remoteOffset)
        {
            return new UdpConnection (localPort: this, remoteHost: remoteHost, remoteOffset: remoteOffset);
        }

        private UdpClient CreateSocket ()
        {
            //Log.Message ("Creating socket localhost:", LocalPort, " <-> ", RemoteHost, ":", RemotePort, "...");
            Log.Message ("Creating socket localhost:", Port, "...");
            UdpClient sock = new UdpClient ();
            sock.ExclusiveAddressUse = false;
            sock.Client.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            IPEndPoint localpt = new IPEndPoint (IPAddress.Any, Port);
            sock.Client.Bind (localpt);
            return sock;
        }

        public void Send (byte[] bytes, ConnectionID cid, IPEndPoint remote)
        {
            Log.Debug ("send (", cid, "): ", Encoding.ASCII.GetString (bytes));
            byte[] _bytes = CollectionExtensions.Combine (BitConverter.GetBytes (cid.ID), bytes);
            Socket.Send (_bytes, _bytes.Length, remote);
        }

        public async Task<UdpReceiveResult> ReceiveAsync (IPEndPoint remote)
        {
            return await Socket.ReceiveAsync ();
        }

        public void RegisterConnection (ConnectionID cid, UdpConnection connection)
        {
            KnownConnections [cid] = connection;
            Queues [cid] = new Queue<Packet> ();
        }

        private async Task ProcessIO ()
        {
            while (true) {
                UdpReceiveResult receivedResults = await Socket.ReceiveAsync ();
                byte[] buffer = receivedResults.Buffer;
                IPEndPoint remote = receivedResults.RemoteEndPoint;
                ConnectionID cid = new ConnectionID { ID = BitConverter.ToInt32 (buffer, 0) };
                Packet packet = new Packet { CID = cid, Buffer = buffer.Skip (4).ToArray () };
                if (HolePunchingUtil.IsKeepAlivePacket (receivedResults.Buffer)) {
                    Log.Debug ("ProcessIO: Keep-Alive");
                } else if (KnownConnections.ContainsKey (cid)) {
                    Log.Debug ("receive (known: ", cid, "): ", Encoding.ASCII.GetString (packet.Buffer));
                    Queues [cid].Enqueue (packet);
                } else {
                    Log.Debug ("receive (unknown: ", cid, "): ", Encoding.ASCII.GetString (packet.Buffer));
                    Queues [ConnectionID.Unknown].Enqueue (packet);
                }
            }
        }

        public bool HasPacketFor (ConnectionID cid)
        {
            return Queues [cid].Count () > 0;
        }

        public Packet GetPacketFor (ConnectionID cid)
        {
            return Queues [cid].Dequeue ();
        }
    }

    public class UdpConnection
    {
        public LocalPort Local { get; private set; }

        public int LocalOffset { get { return Local.Offset; } }

        public ushort LocalPort { get { return Local.Port; } }

        public int RemoteOffset { get; private set; }

        public ushort RemotePort { get { return NetworkUtils.CurrentPort (RemoteOffset); } }

        public string RemoteHost { get; private set; }

        public IPEndPoint RemoteEndPoint { get; private set; }

        private ConnectionID _cid;

        public ConnectionID CID {
            get {
                return _cid;
            }
            private set {
                _cid = value;
                if (!_cid.IsUnknown) {
                    Local.RegisterConnection (cid: _cid, connection: this);
                }
            }
        }

        public UdpConnection (LocalPort localPort, string remoteHost, int remoteOffset)
        {
            Local = localPort;
            RemoteOffset = remoteOffset;
            RemoteHost = remoteHost;
            RemoteEndPoint = CreateEndPoint ();
            Log.Message ("Creating connection localhost:", LocalPort, " <-> ", RemoteHost, ":", RemotePort, "...");
            if (localPort.Offset > remoteOffset) {
                CID = new ConnectionID { ID = HolePunchingUtil.Random.Next () };
                Log.Debug ("Defined connection id: ", CID);
            } else {
                CID = ConnectionID.Unknown;
                Log.Debug ("Unknown connection id (hoping to get one)");
            }
        }

        private IPEndPoint CreateEndPoint ()
        {
            IPAddress peerAddr;
            try {
                peerAddr = IPAddress.Parse (RemoteHost);
            } catch (FormatException) {
                peerAddr = Dns.GetHostAddresses (RemoteHost) [0];
            }
            return new IPEndPoint (peerAddr, RemotePort);
        }

        public void PunchHole ()
        {
            NatTraverse nattra = new NatTraverse (connection: this);
            nattra.Punch ();
        }

        public void Send (byte[] bytes)
        {
            Local.Send (bytes: bytes, cid: CID, remote: RemoteEndPoint);
        }

        public void Send (byte[] bytes, int length)
        {
            Send (bytes.Take (length).ToArray ());
        }

        public void Send (string str)
        {
            byte[] bytes = Encoding.ASCII.GetBytes (str);
            Send (bytes: bytes);
        }

        public void SendError (params object[] message)
        {
            Send ("ERROR:" + string.Join ("", message));
        }

        public async Task<Packet> ReceiveAsync ()
        {
            Packet packet;
            if (Local.HasPacketFor (CID)) {
                packet = Local.GetPacketFor (CID);
            } else {
                while (!Local.HasPacketFor (CID)) {
                    await Task.Delay (10);
                }
                packet = Local.GetPacketFor (CID);
            }

            if (CID.IsUnknown) {
                CID = packet.CID;
                Log.Debug ("Got connection id: ", CID);
            }

            return packet;
        }

        public void SendKeepAlivePackets (Func<bool> whileTrue, CancellationToken token)
        {
            HolePunchingUtil.SendKeepAlivePackets (udp: Local.Socket, udpRemote: RemoteEndPoint, checkIfRunning: whileTrue, token: token);
        }
    }

    public struct Packet
    {
        public ConnectionID CID;
        public byte[] Buffer;

        private string _bufferString;

        public string BufferString { get { return _bufferString = _bufferString ?? Encoding.ASCII.GetString (Buffer); } }

        public override string ToString ()
        {
            return string.Format ("[Packet(cid={0},buffer={1})]", CID, BufferString);
        }

        public bool IsErrorPacket { get { return BufferString.StartsWith ("ERROR:"); } }

        public string ErrorString { get { return IsErrorPacket ? BufferString.Substring (6) : ""; } }
    }

    public struct ConnectionID
    {
        public int ID;

        public static ConnectionID Unknown = new ConnectionID { ID = -1 };

        public bool IsUnknown { get { return ID == -1; } }

        public override string ToString ()
        {
            return string.Format ("[{0}]", ID);
        }
    }
}

