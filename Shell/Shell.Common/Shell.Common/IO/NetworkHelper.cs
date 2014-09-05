using System;

namespace Shell.Common.IO
{
    public static class NetworkHelper
    {
        public static void DisableCertificateChecks ()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
        }
    }
}

