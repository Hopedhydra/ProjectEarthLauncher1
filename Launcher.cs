using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEarthLauncher
{
    internal static class Launcher
    {
        public static void Launch()
        {
            if (!Utils.TryPickFolder("Select install folder", out string installDir))
            {
                Logger.PAKC("You need to select install folder");
                return;
            }

            try
            {
                // only need installDIr
                Context context = new Context(null, installDir, null);

                // make sure api exe exists
                bool isReleasse = context.Api_IsRelease();
                string apiExePath = isReleasse ? Path.Combine(context.ApiDir, "ProjectEarthServerAPI.exe") : Path.Combine(context.Api_BuildPath(), "ProjectEarthServerAPI.exe");
                if (!File.Exists(apiExePath))
                {
                    Logger.Error($"File \"{apiExePath}\" doesn't exist");
                    Logger.PAKC();
                }

                // start api
                ProcessStartInfo processInfo = new ProcessStartInfo(apiExePath);
                processInfo.CreateNoWindow = false;
                processInfo.WindowStyle = ProcessWindowStyle.Normal;
                processInfo.UseShellExecute = true;
                processInfo.WorkingDirectory = Path.GetDirectoryName(apiExePath);
                Process.Start(processInfo);
                Logger.PAKC("Wait for api to start");

                // optionally start cloudburst
                string cloudburst = Path.Combine(context.CloudburstDir, "run.bat");
                if (File.Exists(cloudburst))
                {
                    processInfo = new ProcessStartInfo("cmd.exe", $"/c {File.ReadAllText(cloudburst)}");
                    processInfo.CreateNoWindow = false;
                    processInfo.WindowStyle = ProcessWindowStyle.Normal;
                    processInfo.UseShellExecute = true;
                    processInfo.WorkingDirectory = context.CloudburstDir;
                    Process.Start(processInfo);
                    Logger.Debug("Cloudburst started");
                }

                // optionally start tile server
                string tileServer = Path.Combine(context.TileServerDir, "start.bat");
                if (File.Exists(tileServer))
                {
                    processInfo = new ProcessStartInfo("cmd.exe", $"/c {File.ReadAllText(tileServer)}");
                    processInfo.CreateNoWindow = false;
                    processInfo.WindowStyle = ProcessWindowStyle.Normal;
                    processInfo.UseShellExecute = true;
                    processInfo.WorkingDirectory = context.TileServerDir;
                    Process tileServerProcess = Process.Start(processInfo);
                    Thread.Sleep(1500);
                    if (tileServerProcess.HasExited)
                    {
                        Logger.Error("Failed to launch TileServer, make sure docker is running by opening Docker Desktop");
                        return;
                    }
                    else
                        Logger.Debug("TileServer started");
                }

                Logger.PAKC("Started");
            }
            catch (Exception ex)
            {
                Logger.Info($"There was an exception:");
                Logger.Exception(ex);
                Logger.PAKC();
            }
        }
    }
}
