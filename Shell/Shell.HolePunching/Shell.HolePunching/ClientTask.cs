using System;
using Shell.Common.Tasks;
using System.Net;
using System.Net.Sockets;
using Shell.Common;

namespace Shell.HolePunching
{
    public class ClientTask : Task, MainTask
    {
        public ClientTask ()
        {
            Name = "HolePunchingClient";
            Description = "Run the hole punching client test";
            Options = new string[] { "hole-punching-client-test", "hp-client-test" };
            ConfigName = "HolePunching";
            ParameterSyntax = "";
        }

        protected override void InternalRun (string[] args)
        {
            int serverPort = NetworkUtils.CurrentPort (0);
            int localPort = NetworkUtils.CurrentPort (int.Parse (args [1]));
            IPAddress serverIpAddress = IPAddress.Parse (args [0]);

            IPEndPoint localEndPoint = new IPEndPoint (IPAddress.Any, localPort);
            HolePunchingClient hpc = new HolePunchingClient (localEndPoint);

            IPEndPoint remoteEndPoint = new IPEndPoint (serverIpAddress, serverPort);
            hpc.Connect (remoteEndPoint);

            Console.WriteLine ("Insert ip_address of natted host: ");
            IPAddress remoteAddress = IPAddress.Parse (Console.ReadLine ());

            Socket s = hpc.HolePunch (remoteAddress);

            Console.WriteLine ("In main, my socket is: local ---> " + s.LocalEndPoint + ", remote ---> " + s.RemoteEndPoint);

        }
    }
}

