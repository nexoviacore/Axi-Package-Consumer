using System.Net.Http.Json;
using System.Runtime;
using AxiInstallConsumerPackage.Helpers;
using AxiInstallConsumerPackage.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AxiInstallConsumerPackage.Services
{
    public class PackageImportService : IPackageImportService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PackageImportService> _logger;
        private readonly ApiService _apiService;
        private readonly SignalRClass _signalR;
        private readonly RedisService _redisservice;

        private string packageName;
        private string signalrUrl;
        private string project;
        private string username;

        private string apiResp;

        public PackageImportService(
            IConfiguration configuration,
            ILogger<PackageImportService> logger,
            ApiService apiService,
            SignalRClass signalr,
            RedisService redisservice)
        {
            _configuration = configuration;
            _logger = logger;
            _apiService = apiService;
            _signalR = signalr;
            _redisservice = redisservice;
        }

        public async Task ProcessPackageImport(string queueData)
        {
            try
            {
                _logger.LogInformation("InstallPackage process started.###");

                JObject queueJson = JObject.Parse(queueData);

                project = queueJson["appName"].ToString();

                packageName = queueJson["packageName"].ToString();

                username = queueJson["requestedBy"].ToString();

                signalrUrl = _configuration["ApiUrls:SignalRURL"];

                if (string.IsNullOrEmpty(project))
                {
                    _logger.LogInformation("Project/App Name is missing.");
                    return;
                }

                if (string.IsNullOrEmpty(packageName))
                {
                    _logger.LogInformation("Package Name is missing.");
                    return;
                }

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogInformation("username is missing.");
                    return;
                }

                if (string.IsNullOrEmpty(signalrUrl))
                {
                    _logger.LogInformation("SignalR URL is missing.");
                    return;
                }

                string installpkgkey = $"{project}-installedpkg_{packageName}";
                string inprogresskey = $"{project}-inprogresspkg_{packageName}";

                bool installData = await InstallData(queueData);

                _logger.LogInformation("InProgressKey Set");
                await _redisservice.DeleteKey(inprogresskey, project);

                if (!installData)
                {
                    _logger.LogError("InstallPackage failed. Message : {Message}", apiResp);

                    throw new Exception($"The AXI {packageName} package could not be installed. Please review the installation logs for more details and try again. If the issue persists, contact your administrator.");
                }

                bool dbService = await UpdateAxiControl();

                if(!dbService)
                {
                    throw new Exception($"The AXI {packageName} package was installed successfully, but the installed package information could not be updated in the database. Please contact your administrator.");
                }

                else
                {
                    _logger.LogInformation("InstallKey Set");

                    await _redisservice.SetRedisKey(installpkgkey, packageName, "720", project);
                }

                string SRPayload;

                SRPayload = prepSRPayload($"Package Installed Successfully: {packageName}", $"The AXI {packageName} package has been installed successfully and is now ready to use.");

                await _signalR.SendSignalRMessage(signalrUrl, project, username, SRPayload);

                _logger.LogInformation("InstallPackage completed successfully.");
            }

            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during InstallPackage processing.");

                WriteErrorLog("Error in InstallPackage", ex);
            }
        }

        private async Task<bool> InstallData(string queueData)
        {
            try
            {
                _logger.LogInformation("InstallPackage started.");

                string apiUrl = _configuration["ApiUrls:axinstallapi_url"];

                JObject installJson = new JObject();

                JObject queueJson = JObject.Parse(queueData);

                installJson = PrepareDataPayload(queueJson);

                _logger.LogInformation("Payload:\n"+installJson);

                ApiResponse response = await _apiService.PostAsync(apiUrl, installJson);

                apiResp = response.Message;

                if (!response.Success)
                {
                    _logger.LogError("InstallPackage failed. Message : {Message}", response.Message);

                    return false;
                }

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex.ToString(),
                    "Error in InstallData.");

                WriteErrorLog($"Failed to Install Package: {packageName}", ex);

                return false;
            }
        }

        
        private JObject PrepareDataPayload(JObject queueData)
        {
            JObject data = queueData;

            JObject payloaddata = new JObject();

            payloaddata["project"] = project;

            payloaddata["username"] = username;

            packageName = data["packageName"].ToString();

            string command = $"Install Package \"{packageName}\" ";

            payloaddata["command"] = command;

            payloaddata["trace"] = true;

            return payloaddata;
        }

        public async Task<Boolean> UpdateAxiControl()
        {
            try {
                _logger.LogInformation("AxiClient API Calling");

            var baseUrl = _configuration["AxiClientAPI"];
            string url = baseUrl + "/api/AxiClient/AxiUpdatePackages";

            JObject payload = new JObject();

                payload["AxiAccId"] = project;
                payload["PackageName"] = packageName;

            _logger.LogInformation($"Payload for DB:\n {payload}");

            var response = await _apiService.PostAsync(url, payload);

            if (!response.Success || response is null)
            {
                _logger.LogError("DBService failed. Message : {Message}", response?.Message);

                return false;
            }

                _logger.LogInformation("AxiControl/AxiAccount Updated Succesfully");

            return true;
            }

            catch(Exception ex)
            {
                _logger.LogError("UpdateAxiControl failed. Error : {Message}", ex.Message);
                return false;
            }
        }

        public string prepSRPayload(string title, string msg)
        {
            try
            {

                if (project != null && username != null && signalrUrl != null)
                {
                    JArray jArr = new JArray(
                        new JObject(
                            new JProperty("notifytype", "AxiImportPackage"),
                            new JProperty("type", "TStruct"),
                            new JProperty("icon", ""),
                            new JProperty("title", title),
                            new JProperty("message", msg),
                            new JProperty("dt", DateTime.Now.ToString()),
                            new JProperty("link", new JObject(
                                new JProperty("t", ""),
                                new JProperty("name", ""),
                                new JProperty("p", ""),
                                new JProperty("act", "load"),
                                new JProperty("axmsgid", "")
                            ))
                        )
                    );

                    string _srPayload = jArr.ToString();
                    _srPayload = JsonConvert.SerializeObject(new[]
                        {
                            new
                            {
                                notifytype = "AxiExportPackage",
                                type = "TStruct",
                                icon = "",
                                title,
                                message = msg,
                                dt = DateTime.Now.ToString(),
                                link = new
                                {
                                    t = "",
                                    name = "",
                                    p = "",
                                    act = "load",
                                    axmsgid = ""
                                }
                            }
                        });

                    return _srPayload;
                }

                return null;
            }

            catch (Exception ex)
            {
                _logger.LogError("Error in prepareSRPaylod ", ex);

                return null;
            }
        }

        private void WriteErrorLog(string title, Exception ex)
        {
            try
            {
                string logFolder =
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorLogs");

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                string logFile =
                    Path.Combine(
                        logFolder,
                        $"errorlog_{DateTime.Now:yyyyMMdd}.txt");

                string errorMessage =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                    $"Method: {title}{Environment.NewLine}" +
                    $"Message: {ex.Message}{Environment.NewLine}" +
                    $"StackTrace: {ex.StackTrace}{Environment.NewLine}" +
                    $"--------------------------------------------------{Environment.NewLine}";

                File.AppendAllText(logFile, errorMessage);

                string errMsg = ex.Message;

                _signalR.SendSignalRMessage(signalrUrl, project, username, prepSRPayload(title, errMsg));
            }
            catch
            {
                File.AppendAllText("fallback_error.log",
                "Logging failed: " + ex.Message + " | Original: " + title + Environment.NewLine);
            }
        }
    }
}