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

        public static double ToMillisecondsTimestamp (this DateTime dateTime)
        {
            return (dateTime - new DateTime (1970, 1, 1).ToLocalTime ()).TotalMilliseconds;
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

        public static DateTime StartOfYear (int year)
        {
            return new DateTime (year, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        }

        public static DateTime EndOfYear (int year)
        {
            System.DateTime dt = StartOfYear (year + 1);
            dt = dt.AddSeconds (-1);
            return dt;
        }

        // works for DateTime's and any other class that implements IComparable!
        public static bool Between (this IComparable a, IComparable b, IComparable c)
        {
            return a.CompareTo (b) >= 0 && a.CompareTo (c) <= 0;
        }

        public static bool IsInYear (this DateTime dateTime, int year)
        {
            return dateTime.Between (StartOfYear (year), EndOfYear (year));
        }

        public static bool HasTimeComponent (this DateTime dateTime)
        {
            return !(dateTime.Hour == 0 && dateTime.Minute == 0 && dateTime.Second == 0);
        }

        public static string StdFormat (this DateTime dateTime)
        {
            return dateTime.ToString (format: "yyyyMMdd_HHmmss");
        }
    }
}