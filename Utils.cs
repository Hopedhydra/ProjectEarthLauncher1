#if WINDOWS
using System.Windows.Forms;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel;
using System.Globalization;
using SystemPlus.Extensions;
using SystemPlus;
using System.IO.Compression;
using System.Net.Sockets;
using Newtonsoft.Json;
using LibGit2Sharp;
using System.Runtime.InteropServices;
using static SystemPlus.Extensions.ConsoleExtensions;
using System.Diagnostics;

namespace ProjectEarthLauncher
{
    internal static class Utils
    {
        public static readonly HttpClient Client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        public static bool TryPickFolder(string title, out string path)
        {
#if WINDOWS
        pick:
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = title;
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                path = dialog.SelectedPath;
                if (Directory.Exists(path))
                    return true;
                else
                {
                    Logger.Error($"Folder \"{path}\" doesn't exist");
                    goto pick;
                }
            } else
            {
                path = null;
                return false;
            }
#else
        pick:
            path = Input.String(title);
            if (string.IsNullOrEmpty(path)) 
                return false;
            else if (Directory.Exists(path))
                return true;
            else
            {
                Logger.Error("Folder doesn't exist");
                goto pick;
            }
#endif
        }

        private static long GetRemoteFileSize(string url)
        {
            WebClient client = new WebClient();
            Stream s = client.OpenRead(url);
            long totalSize = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
            s.Close();
            client.Dispose();
            return totalSize;
        }

        public static void DownloadFile(string url, string path, string displayName)
        {
            const int barLenght = 20;

            string downloadDir = Path.GetDirectoryName(path);
            // only creates if it doesn't exist
            Directory.CreateDirectory(downloadDir);

            bool downloaded = false;
            bool cancelled = false;
            bool printing = false;

            Console.CursorVisible = false;

            Logger.Debug($"Starting download for: {displayName}...");

            long totalSize = GetRemoteFileSize(url);
            double lastReceived = 0d;

            Logger.Debug($"Starting download for: {displayName} 0/{(totalSize < 0 ? "?" : totalSize.ToString())}", true);

            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
                {
                    if (printing)
                        return;

                    printing = true;
                    long bytesReceived = e.BytesReceived;
                    double received = IOExtensions.ChooseAppropriate(bytesReceived, IOExtensions.Unit.B, out IOExtensions.Unit ru);
                    string sReceived = MathPlus.Round(received, 2).ToString();
                    // Make sure lenght is constant
                    if (!sReceived.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator))
                        sReceived += CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "00";
                    else if (sReceived.Split(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)[1].Length == 1)
                        sReceived += 0;
                    string sBytesReceived = $"{sReceived}{ru}";
                    if (totalSize == 0 && e.TotalBytesToReceive > 0)
                        totalSize = e.TotalBytesToReceive;

                    if (totalSize > 0)
                    {
                        int currentBarLenght = (int)(((float)e.BytesReceived / totalSize) * (float)barLenght);

                        double total = IOExtensions.ChooseAppropriate(totalSize, IOExtensions.Unit.B, out IOExtensions.Unit tu);
                        string sTotal = MathPlus.Round(total, 2).ToString();
                        // Make sure lenght is constant
                        if (!sTotal.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator))
                            sTotal += CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "00";
                        else if (sTotal.Split(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)[1].Length == 1)
                            sTotal += 0;
                        string sBytesTotal = $"{sTotal}{tu}";

                        Logger.Debug($"Downloading {displayName} {new string('■', currentBarLenght)}{new string('-', barLenght - currentBarLenght)}" +
                            $" {sBytesReceived}/{sBytesTotal} {e.ProgressPercentage}%", true);
                    }
                    else
                    {
                        Logger.Debug($"Downloading {displayName} {sBytesReceived}/?", true);
                    }
                    printing = false;

                    if (e.BytesReceived > lastReceived)
                        lastReceived = e.BytesReceived;
                };
                wc.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) =>
                {
                    cancelled = e.Cancelled;
                    downloaded = true;
                };
                wc.DownloadFileAsync(new Uri(url), path);
            }

            while (!downloaded || printing) { Thread.Sleep(0); }

            double rec = IOExtensions.ChooseAppropriate(totalSize != 0 ? totalSize : lastReceived, IOExtensions.Unit.B, out IOExtensions.Unit u);

            if (cancelled)
                throw new Exception($"Failed download \"{displayName}\"");

            Logger.Debug($"Downloaded {displayName} {MathPlus.Round(rec, 2)}{u} 100%", true);

            Console.CursorVisible = true;
        }

        public static void ExtractZip(string path, Func<string, bool> writeFile, bool deleteZip = true)
        {
            using (FileStream zipStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

                string basePath = Path.GetDirectoryName(path)!;

                bool split = false;
                if (zip.Entries.FirstOrDefault().FullName.EndsWith('/'))
                {
                    split = true;
                    string start = zip.Entries.FirstOrDefault().FullName;
                    foreach (ZipArchiveEntry entry in zip.Entries) 
                        if (!entry.FullName.StartsWith(start))
                            split = false;
                }

                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    string entryName = split ? entry.FullName.Substring(entry.FullName.IndexOf('/') + 1) : entry.FullName;
                    if (string.IsNullOrWhiteSpace(entryName)) continue;
                    else if (entry.FullName.EndsWith('/'))
                    {
                        Directory.CreateDirectory(Path.Combine(basePath, entryName));
                        continue;
                    }

                    string savePath = Path.Combine(basePath, entryName);

                    if (writeFile != null && !writeFile.Invoke(savePath))
                        continue;

                    using FileStream fs = File.OpenWrite(savePath);
                    using Stream entryStream = entry.Open();
                    entryStream.CopyTo(fs);
                }
            } // zipStream needs to be closed before file can be deleted

            if (deleteZip)
                File.Delete(path);
        }

        public static IPEndPoint GetLocalIP()
        {
            IPAddress ip = null;
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    ip = endPoint.Address;
                }
            }
            catch { }

            if (ip is null)
            {
                try
                {
                    ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
                }
                catch { }
            }

            bool askedToChangeIp = false;

            if (ip != null && !Input.YN($"Is this your ip (will be used for server) \"{ip}\"", true))
            {
                ip = null;
                askedToChangeIp = true;
            }

            if (ip == null)
            {
                if (askedToChangeIp)
                    Logger.Info("Please enter your ip");
                else
                    Logger.Info("Couldn't get your ip, please enter it manually");
                getIp:
                string _ip = Input.String("Server's IP (192.168.x.x)");
                if (!IPAddress.TryParse(_ip, out ip))
                {
                    Logger.Error("Couldn't parse ip");
                    goto getIp;
                }
            }

            ushort port = 80;
            if (Input.YN("Do you want to change port (default is 80)", false))
            {
            getPort:
                string read = Input.String("Server's port (0 - 65535)"); // ushort.MaxValue
                if (ushort.TryParse(read, out ushort _port))
                    port = _port;
                else
                {
                    Logger.Info("Couldn't parse ip, make sure it's in range");
                    goto getPort;
                }
            }

            return new IPEndPoint(ip, port);
        }

        public static void DownloadGitRepo(string url, string path, string displayName)
        {
            Logger.Info($"Downloading GitRepo: {url}...");
            Repository.Clone(url, path, new CloneOptions
            {
                RecurseSubmodules = true // Clone submodules recursively
            });
            Logger.Info($"Downloaded GitRepo: {url}", true);
        }

        // needed because some files can be readonly
        public static void DeleteDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            string[] files = Directory.GetFiles(directoryPath);
            string[] directories = Directory.GetDirectories(directoryPath);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in directories)
            {
                DeleteDirectory(dir);
            }

            File.SetAttributes(directoryPath, FileAttributes.Normal);

            Directory.Delete(directoryPath, false);
        }

#if WINDOWS
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHFileOperation([In, Out] SHFILEOPSTRUCT lpFileOp);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;
            public ushort fFlags;
            public int fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;
        }
#endif
        public static void MoveFolderContents(string from, string to)
        {
#if WINDOWS
            const uint FO_MOVE = 0x0001;

            SHFILEOPSTRUCT fileOp = new SHFILEOPSTRUCT
            {
                wFunc = FO_MOVE,
                pFrom = from + "\\*\0\0",
                pTo = to + "\0\0",
                fFlags = 0
            };

            int result = SHFileOperation(fileOp);

            if (result != 0)
                throw new Exception($"Move operation failed. Error code: {result}");
#else
            throw dont compile
#endif
        }

        public static Process RunCommand(string command, string workingDir, bool redirectInput, Action<string, bool> output = null)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardInput = true;
            processInfo.WorkingDirectory = workingDir;

            Process process = Process.Start(processInfo);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (output != null)
            {
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => output.Invoke(e.Data, false);
                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => output.Invoke(e.Data, true);
            }

            return process;
        }

        public static void CopyDir(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyDir(diSource, diTarget);
        }

        // https://stackoverflow.com/a/690980/15878562, all answers are controvertial besides this one, fr is it really that hard to copy dirs 
        public static void CopyDir(DirectoryInfo source, DirectoryInfo target)
        {
            target.Create();

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyDir(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
