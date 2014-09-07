using System;
using Shell.Common.Tasks;
using System.Net;
using System.Net.Sockets;
using Shell.Common;
using Shell.Common.IO;
using Shell.Common.Util;
using System.Diagnostics;
using System.Threading;

namespace Shell.HolePunching
{
    public class TestTask : ScriptTask, MainScriptTask
    {
        public TestTask ()
        {
            Name = "HolePunching";
            Description = "Run the test";
            Options = new string[] { "test" };
            ConfigName = "HolePunching";
            ParameterSyntax = "";
        }

        protected override void InternalRun (string[] args)
        {
            TcpClient tcp = new TcpClient (new IPEndPoint (IPAddress.Any, int.Parse(args[0])));
            while (true) {
                try {
                    tcp.Connect (args [1], int.Parse (args [2]));
                } catch (SocketException ex) {

                    Log.Message (ex.Message);
                }
                Thread.Sleep (2000);
            }
        }
    }
}

