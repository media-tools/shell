using System;

namespace Shell.Common
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfDay (this DateTime theDate)
        {
            return theDate.Date;
        }

        public static DateTime EndOfDay (this DateTime theDate)
        {
            return theDate.Date.AddDays (1).AddTicks (-1);
        }

        public static double ToUnixTimestamp (this DateTime dateTime)
        {
            return (dateTime - new DateTime (1970, 1, 1).ToLocalTime ()).TotalSeconds;
        }
    }
}

