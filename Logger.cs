using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEarthLauncher
{
    internal static class Logger
    {
        private static int lastYPos = -1;
        private static object consoleLock = new object();

        private static ConsoleColor LevelToColor(Level level)
        {
            switch (level)
            {
                case Level.Debug:
                    return ConsoleColor.DarkGray;
                case Level.Info:
                case Level.Input:
                case Level.Input_YN:
                    return ConsoleColor.Gray;
                case Level.Warning:
                case Level.Input_YNWarning:
                    return ConsoleColor.Yellow;
                case Level.Error:
                    return ConsoleColor.Red;
                default:
                    return ConsoleColor.Gray;
            }
        }

        internal static void Log(string message, Level level, bool overwrite = false)
        {
            if (lastYPos < 0)
                overwrite = false;

            lock (consoleLock)
            {
                if (overwrite)
                    Console.CursorTop = lastYPos;

                lastYPos = Console.CursorTop;
                Console.Write($"[{DateTime.UtcNow.ToString("HH:mm:ss")}] [{level,-8}] ");
                Console.ForegroundColor = LevelToColor(level);
                Console.Write(message);
                //if (overwrite)
                //    Console.Write(new string(' ', Console.BufferWidth - Console.CursorLeft));
                if ((byte)level <= (byte)Level.Error)
                {
                    Console.Write(new string(' ', Math.Max(0, Console.BufferWidth - (Console.CursorLeft + 2))));
                    Console.WriteLine();
                }
                Console.ResetColor();
            }
        }

        public static void Debug(string message, bool overwrite = false)
            => Log(message, Level.Debug, overwrite);
        public static void Info(string message, bool overwrite = false)
            => Log(message, Level.Info, overwrite);
        public static void Warning(string message, bool overwrite = false)
            => Log(message, Level.Warning, overwrite);
        public static void Error(string message, bool overwrite = false)
            => Log(message, Level.Error, overwrite);
        public static void Exception(Exception exception)
            => Log(exception.ToString(), Level.Error);

        public static void PAKC(string message = "")
        {
            if (string.IsNullOrEmpty(message))
            {
                Info("Press any key to continue...");
                Console.ReadKey(true);
            } else
            {
                Info(message + ", press any key to continue...");
                Console.ReadKey(true);
            }
        }

        internal enum Level : byte
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,

            Input = 4,
            Input_YN = 5,
            Input_YNWarning = 6,
        }
    }
}
