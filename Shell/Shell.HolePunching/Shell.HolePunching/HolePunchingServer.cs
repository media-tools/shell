using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace HolePunching
{
    public class HolePunchingServer
    {
        private Socket _socket;
        private List<KeyValuePair<IPEndPoint, Socket> > _registeredList;

        public HolePunchingServer (int port, int backlog)
        {
            _registeredList = new List<KeyValuePair<IPEndPoint, Socket>> ();

            _socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind (new IPEndPoint (IPAddress.Any, port));
            _socket.Listen (backlog);
        }

        private void ProcessRequest (object sender, DoWorkEventArgs eventArgs)
        {
            Socket tempSocket = (Socket)eventArgs.Argument;
            byte[] bytes = new byte[2048];

            /*
             
             * ToDo: handle the exception that is thrown in the server when the client close the socket and the server is still reading this socket.
             * 
             * Possible solution: Make every part of the switch (register, request, unregister) as functions,
             * then call the unregister function when a SocketException (is this the exception?) is thrown. 
             
             */
            while (true) {
                tempSocket.Receive (bytes);
                if (!tempSocket.Connected)
                    return;
                IPEndPoint remoteEndPoint = (IPEndPoint)tempSocket.RemoteEndPoint;

                MessageType messageType = (MessageType)(bytes [0]);

                Console.WriteLine ("MessageType: " + messageType);

                switch (messageType) {
                case MessageType.Register:
                    Console.WriteLine ("Registration for: " + remoteEndPoint);
                    if (!_registeredList.Exists (kvp => kvp.Key == remoteEndPoint))
                        _registeredList.Add (new KeyValuePair<IPEndPoint, Socket> (remoteEndPoint, tempSocket));
                    break;

                case MessageType.RequestClient:
                    byte[] byteAddress = new byte[4];

                    Buffer.BlockCopy (bytes, 1, byteAddress, 0, 4);

                        /*IpEndPoint requested to hole punching*/
                    IPAddress requestedAddress = new IPAddress (byteAddress);
                    KeyValuePair<IPEndPoint, Socket> keyValuePair = _registeredList.Where (kvp => kvp.Key.Address == requestedAddress).ElementAt (0);
                    IPEndPoint requestedEndPoint = keyValuePair.Key;
                    Socket requestedSocket = keyValuePair.Value;

                    Console.WriteLine (remoteEndPoint + " requested parameters of: " + requestedEndPoint);

                        /*Sending informations about endpoints*/
                    byte[] connectRequestedClient = new byte[7];
                    Buffer.BlockCopy (BitConverter.GetBytes ((byte)MessageType.ConnectClient), 0, connectRequestedClient, 0, 1);
                    Buffer.BlockCopy (requestedEndPoint.Address.GetAddressBytes (), 0, connectRequestedClient, 1, 4);
                    Buffer.BlockCopy (BitConverter.GetBytes (requestedEndPoint.Port), 0, connectRequestedClient, 5, 2);

                    byte[] connectSenderClient = new byte[7];
                    Buffer.BlockCopy (BitConverter.GetBytes ((byte)MessageType.ConnectClient), 0, connectSenderClient, 0, 1);
                    Buffer.BlockCopy (remoteEndPoint.Address.GetAddressBytes (), 0, connectSenderClient, 1, 4);
                    Buffer.BlockCopy (BitConverter.GetBytes (remoteEndPoint.Port), 0, connectSenderClient, 5, 2);

                    tempSocket.Send (connectRequestedClient);
                    requestedSocket.Send (connectSenderClient);

                    break;

                case MessageType.Unregister:
                    Console.WriteLine ("Unregistration for: " + remoteEndPoint);
                    if (_registeredList.Exists (kvp => kvp.Key == remoteEndPoint))
                        _registeredList.RemoveAll (kvp => kvp.Key == remoteEndPoint);
                    break;
                }
            }
        }

        private void BeginServerThread (object sender, DoWorkEventArgs eventArgs)
        {
            while (true) {
                Socket tempSocket = _socket.Accept ();

                BackgroundWorker backgroundWorker = new BackgroundWorker ();
                backgroundWorker.DoWork += ProcessRequest;
                backgroundWorker.RunWorkerAsync (tempSocket);
            }
        }

        public void BeginServer ()
        {
            BackgroundWorker backgroundWorker = new BackgroundWorker ();
            backgroundWorker.DoWork += BeginServerThread;
            backgroundWorker.RunWorkerAsync ();
        }

    }
}
