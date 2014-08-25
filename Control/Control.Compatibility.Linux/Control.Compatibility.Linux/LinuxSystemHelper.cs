using System;
using Control.Compatibility.Common;
using Mono.Unix.Native;

namespace Control.Compatibility.Linux
{
    public class LinuxSystemHelper : SystemHelper
    {
        public override uint GetUid ()
        {
            return Syscall.getuid();
        }
    }
}

