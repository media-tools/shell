using System;

namespace Shell.Common.Util
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

        public static DateTime UnixTimeStampToDateTime (double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime (1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds (unixTimeStamp).ToLocalTime ();
            return dtDateTime;
        }

        public static DateTime MillisecondsTimeStampToDateTime (double milliTimeStamp)
        {
            // milliseconds past epoch
            System.DateTime dtDateTime = new DateTime (1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds (milliTimeStamp).ToLocalTime ();
            return dtDateTime;
        }
    }
}