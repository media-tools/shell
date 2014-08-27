using System;

namespace Control.FileSync
{
    public static class TimeSpanExtensions
    {
        public static String Verbose (this TimeSpan timeSpan)
        {
            double days = timeSpan.TotalDays;
            double hours = timeSpan.TotalHours;
            int minutes = timeSpan.Minutes;
            int seconds = timeSpan.Seconds;

            if (Math.Floor (days) > 0)
                return String.Format ("{0:0.0} days", days);
            else if (Math.Floor (hours) > 0)
                return String.Format ("{0:0.0} hours", hours);
            else if (minutes > 0)
                return String.Format ("{0} minutes", minutes);
            else
                return String.Format ("{0} seconds", seconds);
        }
    }
}

