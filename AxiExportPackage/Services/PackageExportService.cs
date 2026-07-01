using AxiExportPackage.Models;
using AxiExportPackage.Services.Interfaces;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AxiExportPackage.Services
{
    public class PackageExportService : IPackageExportService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PackageExportService> _logger;
        private readonly ApiService _apiService;
        private readonly SignalRClass _signalR;

        private string exportDir;
        private string packageName;

        private string signalrUrl;
        private string project;
        private string clientId;

        public PackageExportService(
            IConfiguration configuration,
            ILogger<PackageExportService> logger,
            ApiService apiService,
            SignalRClass signalr)
        {
            _configuration = configuration;
            _logger = logger;
            _apiService = apiService;
            _signalR = signalr;
        }

        public async Task ProcessPackageExport(string queueData)
        {
            try
            {
                _logger.LogInformation("Package export process started.");

                JObject queueJson = JObject.Parse(queueData);

                signalrUrl = queueJson["signalrurl"]?.ToString()??_configuration["ExportPath:SignalRURL"]; 
                project = queueJson["appName"].ToString();
                clientId = queueJson["requestedBy"].ToString();

                packageName = queueJson["packageName"].ToString();

                bool structureResult = await ExportStructures(queueData);

                if (!structureResult)
                {
                    _logger.LogError("ExportStructures failed.");

                    return;
                }

                bool dataResult = await ExportData(queueData);

                //dataResult = true;

                if (!dataResult)
                {
                    _logger.LogError("ExportData failed.");

                    return;
                }

                bool gitResult = await PushToGitHub(queueData);

                //gitResult = true;

                if (!gitResult)
                {
                    _logger.LogError("PushToGitHub failed.");

                    return;
                }

                
                //signalr 
                

                _logger.LogInformation("Package export completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during package export processing.");
            }
        }

        private async Task<bool> ExportStructures(string queueData)
        {
            try
            {
                _logger.LogInformation("ExportStructures started.");

                string apiUrl = _configuration["ApiUrls:axiasbdefapi_url"];

                JObject payload = PrepareStructuresPayload(queueData);

               _logger.LogInformation("ExportStructures payload \n"+payload);

                ApiResponse response = await _apiService.PostAsync(apiUrl, payload, "ExportStructures");

                string SRPayload;

                if (!response.Success)
                {
                    _logger.LogError("ExportStructures failed. Message : {Message}", response.Message);

                    throw new Exception($"AXI package {packageName} could not be published to Git.Review the export logs for details and try again. If the issue persists, contact your administrator.");
                    

                    /*SRPayload = prepSRPayload($"Failed to Publish AXI {packageName} Package to Git", $"AXI package {packageName} could not be published to Git. Please check the logs and try again.");//("ExportStructures failed", response.ResponseContent);

                    await _signalR.SendSignalRMessage(signalrUrl,project,clientId,SRPayload);

                    return false;*/
                }

                _logger.LogInformation("ExportStructures completed successfully.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportStructures.");

                WriteErrorLog($"Failed to publish AXI {packageName} Package to Git", ex);

                //_signalR.SendSignalRMessage(signalrUrl, project, clientId, prepSRPayload("Error in ExportStructures API", ex.ToString()));

                return false;
            }
        }

        private async Task<bool> ExportData(string queueData)
        {
            try
            {
                _logger.LogInformation("ExportData started.");

                string apiUrl = _configuration["ApiUrls:axiexportapi_url"];

                JObject payload = PrepareDataPayload(queueData);

                _logger.LogInformation("ExportData payload \n" + payload);

                ApiResponse response = await _apiService.PostAsync(apiUrl, payload, "");

                string SRPayload;

                if (!response.Success)
                {
                    _logger.LogError("ExportData failed. Message : {Message}", response.Message);

                    throw new Exception($"AXI package {packageName} could not be published to Git.Review the export logs for details and try again. If the issue persists, contact your administrator.");

                    /*SRPayload = prepSRPayload($"Failed to Publish AXI {packageName} Package to Git", $"AXI package {packageName} could not be published to Git. Please check the logs and try again.");//("ExportData failed", response.ResponseContent);//

                    await _signalR.SendSignalRMessage(signalrUrl, project, clientId, SRPayload);

                    return false;*/
                }

                _logger.LogInformation("ExportData completed successfully.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportData.");

                WriteErrorLog($"Failed to publish AXI {packageName} Package to Git", ex);

                return false;
            }
        }

        private async Task<bool> PushToGitHub(string queueData)
        {
            try
            {
                _logger.LogInformation("PushToGitHub started.");

                //signalrUrl = _configuration["ExportPath:SignalRURL"];

                string apiUrl = _configuration["ApiUrls:axigitpushapi_url"];

                JObject payload = PrepareGitPayload(queueData);

                _logger.LogInformation("GitPush payload \n" + payload);

                ApiResponse response = await _apiService.PostAsync(apiUrl, payload,"");

                string SRPayload;

                if (!response.Success)
                {
                    _logger.LogError("PushToGitHub failed. Message : {Message}", response.Message);

                    throw new Exception($"AXI package {packageName} could not be published to Git.Review the export logs for details and try again. If the issue persists, contact your administrator.");

                    /*SRPayload = prepSRPayload($"Failed to Publish AXI {packageName} Package to Git", $"AXI package {packageName} could not be published to Git. Please check the logs and try again.");

                    await _signalR.SendSignalRMessage(signalrUrl, project, clientId, SRPayload);

                    return false;*/
                }

                _logger.LogInformation("PushToGitHub completed successfully.");

                SRPayload = prepSRPayload($"AXI {packageName} Package Published to Git", $"The AXI package {packageName} was successfully published to Git and is ready for deployment or testing.") ;

                await _signalR.SendSignalRMessage(signalrUrl, project, clientId, SRPayload);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PushToGitHub.");

                WriteErrorLog($"Failed to publish AXI {packageName} Package to Git", ex);

                return false;
            }
        }

        private JObject PrepareStructuresPayload(string queueData)
        {
            JObject queueJson =
        JObject.Parse(queueData);

            JObject exportObject = new JObject();

            /*
                axpapp
                from queuedata.appName
            */
            exportObject["axpapp"] = queueJson["appName"]?.ToString();

            /*
                username
                from queuedata.requestedId
            */
            exportObject["username"] = queueJson["requestId"]?.ToString();

            /*
                default values
            */
            exportObject["trace"] = true;

            exportObject["printdocs"] = true;

            exportObject["compressedfile"] = true;

            /*
                exppath
                from appsettings.json
            */
            //exportObject["exppath"] = _configuration["ExportPath:exportdir"];

            string expd = _configuration["ExportPath:exportdatapath"];

            if(!string.IsNullOrWhiteSpace(expd)) 
            {
                exportObject["exppath"] = _configuration["ExportPath:exportdatapath"].ToString()+"\\AxiPackages";
                exportDir = exportObject["exppath"].ToString();//_configuration["ExportPath:exportdatapath"].ToString() + "\\AxiPackages";
                Directory.CreateDirectory(exportDir);
            }

            else
            {
                string getDir = Directory.GetCurrentDirectory();

                string folderPath = Path.Combine(
                    getDir,
                    "AxiPackage",
                    queueJson["appName"]?.ToString() ?? "DefaultApp",
                    queueJson["requestedBy"]?.ToString() ?? "DefaultUser",
                    queueJson["packageName"]?.ToString() ?? "DefaultPackage",
                    queueJson["packageVersion"]?.ToString() ?? "1.0"
                );

                Directory.CreateDirectory(folderPath);

                exportObject["exppath"] = folderPath;
                exportDir = folderPath;
            }

            /*
                structs
            */
            JObject structsObject = PrepareStructs(queueJson["objects"] as JObject);

            exportObject["structs"] = structsObject;

            /*
                final payload
            */
            JObject finalPayload = new JObject();

            finalPayload["export"] = exportObject;

            return finalPayload;
        }

        private JObject PrepareDataPayload(string queueData)
        {
            JObject data = JObject.Parse(queueData);

            data.Remove("actions");
            data.Remove("publish");

            data["exportdir"] = exportDir;//_configuration["ExportPath:exportdir"];
            data["password"] = "22723bbd4217a0abf6d3e68073c7603d";

            return data;
        }

        private JObject PrepareGitPayload(string queueData)
        {
            JObject data = JObject.Parse(queueData);

            JObject payload = new JObject();

            payload["physicalPath"] = exportDir;//_configuration["ExportPath:exportdir"];

            payload["targetRepo"] = data["publish"]["repository"];

            payload["packageName"] = data["packageName"];
            ;

            payload["gitbranch"] = data["gitbranch"] ?? data["publish"]["branch"] ?? "main";

            payload["packageDescription"] = data["publish"]["packageDescription"];

            payload["packageVersion"] = data["packageVersion"];

            payload["packageAuthor"] = data["requestedBy"]; 

            return payload;
        }

        private JObject PrepareStructs(JObject objectsJson)
        {
            JObject structsObject = new JObject();

            if (objectsJson == null)
            {
                return structsObject;
            }

            /*
                Process tstruct
            */
            if (objectsJson["tstruct"] is JArray tstructArray)
            {
                JObject tstructObject = new JObject();

                foreach (var item in tstructArray)
                {
                    string value = item.ToString();

                    tstructObject[value] = value;
                }

                structsObject["tstruct"] =
                    tstructObject;
            }

            /*
                Process iview
            */
            if (objectsJson["iview"] is JArray iviewArray)
            {
                JObject iviewObject = new JObject();

                foreach (var item in iviewArray)
                {
                    string value = item.ToString();

                    iviewObject[value] = value;
                }

                structsObject["iview"] =
                    iviewObject;
            }

            if (objectsJson["page"] is JArray pageArray)
            {
                JObject iviewObject = new JObject();

                foreach (var item in pageArray)
                {
                    string value = item.ToString();

                    iviewObject[value] = value;
                }

                structsObject["page"] =
                    iviewObject;
            }

            return structsObject;
        }

        public string prepSRPayload(string title, string msg)
        {
            try
            {

                if (project != null && clientId != null && signalrUrl != null)
                {
                    JArray jArr = new JArray(
                        new JObject(
                            new JProperty("notifytype", "AxiExportPackage"),
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

                    /*string _srPayload = JsonConvert.SerializeObject(new[]
                        {
                            new
                            {
                                notifytype = "AxiExportPackage",
                                type = "TStruct",
                                icon = "",
                                title = title,
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
                        });*/

                    //_srPayload = _srPayload = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(_srPayload);

                    return _srPayload;
                }

                return null;
            }

            catch (Exception ex)
            {
                // Call your custom log method here
                //WriteErrorLog("Error in prepareSRPayload: ", ex.ToString());
                _logger.LogError("Error in prepareSRPaylod ",ex);

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

                _signalR.SendSignalRMessage(signalrUrl, project, clientId, prepSRPayload(title, ex.Message));
            }
            catch
            {
                File.AppendAllText("fallback_error.log",
                "Logging failed: " + ex.Message + " | Original: " + title + Environment.NewLine);
                // Avoid throwing exception from logger itself
            }
        }

        /*private async Task WriteErrorLog(string title, string exceptions)
        {
            _logger.LogError(title+" "+exceptions);

            _signalR.SendSignalRMessage(signalrUrl, project, clientId, prepSRPayload(title, exceptions));
        }*/
    }
}