using System;

namespace Control.Common.IO
{
    [Serializable]
    public enum LogColor
    {
        Black,
        DarkBlue,
        DarkGreen,
        DarkCyan,
        DarkRed,
        DarkMagenta,
        DarkYellow,
        Gray,
        DarkGray,
        Blue,
        Green,
        Cyan,
        Red,
        Magenta,
        Yellow,
        White,
        Reset
    }

    public static class LogColorExtensions
    {
        public static ConsoleColor ToConsoleColor (this LogColor color)
        {
            return color == LogColor.Black ? ConsoleColor.Black :
                color == LogColor.DarkBlue ? ConsoleColor.DarkBlue :
                    color == LogColor.DarkGreen ? ConsoleColor.DarkGreen :
                    color == LogColor.DarkCyan ? ConsoleColor.DarkCyan :
                    color == LogColor.DarkRed ? ConsoleColor.DarkRed :
                    color == LogColor.DarkMagenta ? ConsoleColor.DarkMagenta :
                    color == LogColor.DarkYellow ? ConsoleColor.DarkYellow :
                    color == LogColor.Gray ? ConsoleColor.Gray :
                    color == LogColor.DarkGray ? ConsoleColor.DarkGray :
                    color == LogColor.Blue ? ConsoleColor.Blue :
                    color == LogColor.Green ? ConsoleColor.Green :
                    color == LogColor.Cyan ? ConsoleColor.Cyan :
                    color == LogColor.Red ? ConsoleColor.Red :
                    color == LogColor.Magenta ? ConsoleColor.Magenta :
                    color == LogColor.Yellow ? ConsoleColor.Yellow :
                    color == LogColor.White ? ConsoleColor.White :
                    default(ConsoleColor);
        }
    }
}

