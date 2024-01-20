using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEarthLauncher
{
    internal static class Input
    {
        public static bool YN(string message, bool defaultYes)
        {
            Logger.Log(message + $" ({(defaultYes ? "Y/n" : "y/N")}): ", Logger.Level.Input_YN);
            string input = Console.ReadKey().KeyChar.ToString().ToLowerInvariant();
            Console.WriteLine();
            if (input == "y")
                return true;
            else if (input == "n")
                return false;
            else
                return defaultYes;
        }
        public static bool YNWarning(string message, bool defaultYes)
        {
            Logger.Log(message + $" ({(defaultYes ? "Y/n" : "y/N")}): ", Logger.Level.Input_YNWarning);
            string input = Console.ReadKey().KeyChar.ToString().ToLowerInvariant();
            Console.WriteLine();
            if (input == "y")
                return true;
            else if (input == "n")
                return false;
            else
                return defaultYes;
        }

        public static int Enum(string message, params string[] values)
        {
            Logger.Info(message);
            Logger.Info("Values:");
            for (int i = 0; i < values.Length; i++)
                Logger.Info($"[{i}]: {values[i]}");

        takeInput:
            string inp = String("Value");
            if (int.TryParse(inp, out int selected))
            {
                if (selected >= 0 && selected < values.Length)
                    return selected;
                else
                {
                    Logger.Error($"Values must be between 0 and {values.Length - 1}");
                    goto takeInput;
                }
            } else
            {
                Logger.Error($"\"{inp}\" isn't a valid number");
                goto takeInput;
            }
        }

        public static string String(string message)
        {
            Logger.Log(message + ": ", Logger.Level.Input);
            return Console.ReadLine();
        }
    }
}
