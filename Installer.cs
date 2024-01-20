using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEarthLauncher
{
    internal class Installer
    {
        public void Install(bool api, bool cloudburst, bool tileServer)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            if (CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName != "eng")
            {
                Logger.Warning("Your current ui language isn't english, this might cause problems");
                if (!Input.YN("Do you want to continue anyway?", false))
                    return;
            }

            if (!Utils.TryPickFolder("Select install folder", out string installDir))
            {
                Logger.PAKC("You need to select install folder");
                return;
            }

            try
            {
                string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "config.json");

                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(Config.Default, Formatting.Indented));
                    Logger.PAKC($"Config file was created: {configPath}");
                }

                Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

                InstalationContext context = new InstalationContext(config, installDir, Utils.GetLocalIP());

                if (api) installApi(context);
            } catch (Exception ex)
            {
                Logger.Info($"There was an exception during installation:");
                Logger.Exception(ex);
                Logger.PAKC();
            }
        }

        private bool installApi(InstalationContext context)
        {
            Logger.Info("**********Installing Api**********");

            // trying to install release while source is currenty installed, very bad things would probably happen
            // TODO: if this isn't repo, detect if source or release and do this check
            if (context.Config.api.IsGitRepo && context.Api_IsReleaseNoUI() == 1)
            {
                Logger.PAKC("Api folder will be deleted, because current installation isn't compatible");
                Utils.DeleteDirectory(context.ApiDir);
            }

            if (!Directory.Exists(context.ApiDir) || Input.YN("Api folder already exists, do you want to download it again?", false))
            {
                Directory.CreateDirectory(context.ApiDir);
                // download and extract
                if (context.Config.api.IsGitRepo)
                {
                    string tempPath = Path.Combine(context.InstallDir, "temp");
                    if (Directory.Exists(tempPath))
                        Utils.DeleteDirectory(tempPath);
                    Directory.CreateDirectory(tempPath);

                    Utils.DownloadGitRepo(context.Config.api.Url, tempPath, "Api");

                    Utils.MoveFolderContents(tempPath, context.ApiDir);

                    Utils.DeleteDirectory(tempPath);
                }
                else
                {
                    string apiZip = Path.Combine(context.ApiDir, "api.zip");
                    context.DownloadFile(context.Config.api.Url, apiZip, "Api");

                    context.ExtractZip(apiZip);
                }
            }

            bool isRelease = context.Api_IsRelease();
            string dataPath = isRelease ? Path.Combine(context.ApiDir, "data") : Path.Combine(context.ApiDir, "ProjectEarthServerAPI", "data");

            // config
            string configPath = Path.Combine(dataPath, "config",  "apiconfig.json");
            JObject apiConfig = JObject.Parse(File.ReadAllText(configPath));
            apiConfig["baseServerIP"] = $"http://{context.Ip}";
            apiConfig["useBaseServerIP"] = false;
            apiConfig["multiplayerAuthKeys"] = JObject.FromObject(new Dictionary<string, string>()
            {
                { context.Ip.Address.ToString(), "/g1xCS33QYGC+F2s016WXaQWT8ICnzJvdqcVltNtWljrkCyjd5Ut4tvy2d/IgNga0uniZxv/t0hELdZmvx+cdA==" }
            });

            File.WriteAllText(configPath, apiConfig.ToString());
            Logger.Debug("Edited apiconfig.json");

            // appsettings
            if (context.Ip.Port != 80)
            {
                if (isRelease)
                    Logger.PAKC("Cannot change port that api is running on (80), because api is release. If you want to change it api needs to be source code");
                else
                {
                    string appSettingsPath = Path.Combine(context.ApiDir, "ProjectEarthServerAPI", "appsettings.json");
                    JObject appSettings = JObject.Parse(File.ReadAllText(appSettingsPath));
                    appSettings["Kestrel"]["EndPoints"]["Http"]["Url"] = $"http://*:{context.Ip.Port}";
                    File.WriteAllText(appSettingsPath, appSettings.ToString());
                    Logger.Debug("Edited appsettings.json");
                }
            }

            // resource pack
            context.DownloadFile("https://www.dropbox.com/scl/fi/kbyysugyhb94zj9zb6pgc/vanilla.zip?rlkey=xt6c7nhpbzzyw7gua524ssb40&dl=1", Path.Combine(dataPath, "resourcepacks", "vanilla.zip"), "Resourcepack");

            // build
            if (!isRelease)
            {
                Logger.Debug("Building Api...");
                string buildScriptPath = Path.Combine(context.ApiDir, "ProjectEarthServerAPI", "build.bat");
                context.WriteAllText(buildScriptPath, "dotnet build --nologo --property WarningLevel=0 /clp:ErrorsOnly");

                string commandText = File.ReadAllText(buildScriptPath);
                Logger.Info($"Running: \"{commandText}\"");

                bool gotBuildSucceeded = false;
                Process process = Utils.RunCommand(commandText, Path.Combine(context.ApiDir, "ProjectEarthServerAPI"), true, (string text, bool isError) =>
                {
                    Logger.Debug($"[{(isError ? "err" : "out")}] {text}");
                    if (!isError && text != null && text.Contains("Build succeeded"))
                        gotBuildSucceeded = true;
                });

                // wait max 90s
                process.WaitForExit(90 * 1000);
                process.Close();

                if (!gotBuildSucceeded)
                {
                    Logger.PAKC($"Failed to build api");
                    return false;
                }

                Logger.Debug("Copying data folder...");
                Utils.CopyDir(dataPath, Path.Combine(context.Api_BuildPath(), "data"));
                Logger.Debug("Copied data folder", true);
            }

            Logger.PAKC("Api installed");
            return true;
        }

        public class Config
        {
            public static readonly Config Default = new Config()
            {
                api = new Api()
                {
                    IsGitRepo = true,
                    Url = "https://github.com/jackcaver/Api.git"
                }
            };

            public Api api { get; set; }

            public class Api
            {
                public bool IsGitRepo { get; set; }
                public string Url { get; set; }
            }
        }
    }
}
