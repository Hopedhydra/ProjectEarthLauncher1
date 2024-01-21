﻿using System.Globalization;
using SystemPlus.Extensions;
using SystemPlus.UI;
using SystemPlus.Vectors;

namespace ProjectEarthLauncher
{
    /*
     * TODO:
     * -Input.Enum
     * -add: yes, but ask again; no, but ask again to checkFileAction
     * 
     */
    internal static class Program
    {
#if WINDOWS
        [STAThread]
#endif
        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            while (true)
            {
                prepScreen();
                Menu menu = new Menu(GeneralExtensions.ToBIG("ProjectEarth Launcher 2.0"), new string[] { "Start", "Install", "Exit" });
                int selected = menu.Show(Vector2Int.Up);

                prepScreen();
                switch (selected)
                {
                    case 0:
                        Console.Clear();
                        Launcher.Launch();
                        break;
                    case 1:
                        prepScreen();
                        MenuSettings settings = new MenuSettings("Select which parts to install", new[] {
                            new MSIBool("Api", true) { dispType = 3 },
                            new MSIBool("Cloudburst", true) { dispType = 3 },
                            new MSIBool("TileServer", false) { dispType = 3 },
                        });
                        if (settings.Show(Vector2Int.Up * 2) == MenuSettings.STATUS.OK)
                        {
                            Console.Clear();
                            Installer.Install((bool)settings.lastValues[0].GetValue(), (bool)settings.lastValues[1].GetValue(), (bool)settings.lastValues[2].GetValue());
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        static void prepScreen()
        {
            Console.Clear();
            Console.Write("(Up, Down, Enter)");
        }
    }
}
