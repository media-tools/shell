using System;
using Shell.Common.Tasks;
using Shell.Common;
using System.Net;

namespace Shell.Proxy
{
    public class Socks5ServerTask : Task, MainTask
    {
        public Socks5ServerTask ()
        {
            Name = "Socks5Server";
            Description = "Run the socks 5 server";
            Options = new string[] { "socks-server" };
            ConfigName = "Socks5";
            ParameterSyntax = "";
        }

        protected override void InternalRun (string[] args)
        {
            int port = NetworkUtils.CurrentPort (0);

            Socks5Server f = new Socks5Server (IPAddress.Any, port);
            f.Start ();
        }

    }
}
