using System;

namespace Shell.Common.Util
{

    public static class TimeSpanExtension
    {
        /// <summary>
        /// Multiplies a timespan by an integer value
        /// </summary>
        public static TimeSpan Multiply (this TimeSpan multiplicand, int multiplier)
        {
            return TimeSpan.FromTicks (multiplicand.Ticks * multiplier);
        }

        /// <summary>
        /// Multiplies a timespan by a double value
        /// </summary>
        public static TimeSpan Multiply (this TimeSpan multiplicand, double multiplier)
        {
            return TimeSpan.FromTicks ((long)(multiplicand.Ticks * multiplier));
        }
    }
}