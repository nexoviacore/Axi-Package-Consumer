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

        private string packageName;
        private string signalrUrl;
        private string project;
        private string clientId;
        private string downloadDir;

        public PackageImportService(
            IConfiguration configuration,
            ILogger<PackageImportService> logger,
            ApiService apiService,
            SignalRClass signalr)
        {
            _configuration = configuration;
            _logger = logger;
            _apiService = apiService;
            _signalR = signalr;
        }

        public async Task ProcessPackageImport(string queueData)
        {
            try
            {
                _logger.LogInformation(
                    "Package import process started.###");

                JObject queueJson = JObject.Parse(queueData);

                signalrUrl = queueJson["signalrurl"]?.ToString()??_configuration["ImportPath:SignalRURL"]; 
                project = queueJson["appName"].ToString();
                clientId = queueJson["requestedBy"].ToString();

                packageName = queueJson["packageName"].ToString();

                bool gitResult =
                    await GitPull(queueData);


                if (!gitResult)
                {
                    _logger.LogError(
                        "PushToGitHub failed.");

                    return;
                }

                bool structureResult =
                    await ImportStructures(queueData);

                if (!structureResult)
                {
                    _logger.LogError(
                        "ImportStructures failed.");

                    return;
                }

                bool dataResult =
                    await ImportData(queueData);

                if (!dataResult)
                {
                    _logger.LogError(
                        "ImportData failed.");

                    return;
                }                

                _logger.LogInformation(
                    "Package import completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during package import processing.");

                WriteErrorLog("Error in ImportPackage", ex);
            }
        }

        private async Task<bool> ImportStructures(string queueData)
        {
            try
            {
                _logger.LogInformation(
                    "ImportStructures started.");

                string apiUrl =
                    _configuration["ApiUrls:axiasbdefimpapi_url"];

                JObject payload =
                    PrepareStructuresPayload(queueData);

                _logger.LogInformation("ImportStructures payload \n" + payload);

                ApiResponse response =
                    await _apiService.PostAsync(apiUrl, payload);

                string SRPayload;

                if (!response.Success)
                {
                    _logger.LogError("ImportStructures failed. Message : {Message}", response.Message);

                    throw new Exception($"The AXI  {packageName}  package could not be installed. Please review the installation logs for more details and try again. If the issue persists, contact your administrator ");


                    /*SRPayload = prepSRPayload($"Failed to Publish AXI {packageName} Package to Git", $"AXI package {packageName} could not be published to Git. Please check the logs and try again.");//("ExportStructures failed", response.ResponseContent);

                    await _signalR.SendSignalRMessage(signalrUrl,project,clientId,SRPayload);

                    return false;*/
                }

                _logger.LogInformation("ImportStructures completed successfully.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImportStructures.");

                WriteErrorLog($"Failed to Install Package:  {packageName}", ex);

                //_signalR.SendSignalRMessage(signalrUrl, project, clientId, prepSRPayload("Error in ExportStructures API", ex.ToString()));

                return false;
            }
        }

        private async Task<bool> ImportData(string queueData)
        {
            try
            {
                _logger.LogInformation(
                    "ImportData started.");

                string apiUrl =
                    _configuration["ApiUrls:axiexportapi_url"];

                if (HasAxiDataFile(downloadDir+"\\"+packageName))
                {

                JObject payload =
                    PrepareDataPayload(queueData);

                _logger.LogInformation("ImportData payload \n" + payload);

                ApiResponse response =
                    await _apiService.PostAsync(apiUrl, payload);

                string SRPayload;

                if (!response.Success)
                {
                    _logger.LogError(
                        "ImportData failed. Message : {Message}",
                        response.Message);

                    throw new Exception($"The AXI {packageName} package could not be installed. Please review the installation logs for more details and try again. If the issue persists, contact your administrator.");
                }

                _logger.LogInformation(
                    "ImportData completed successfully.");

                SRPayload = prepSRPayload($"Package Installed Successfully: {packageName}", $"The AXI {packageName} package has been installed successfully and is now ready to use.");

                await _signalR.SendSignalRMessage(signalrUrl, project, clientId, SRPayload);

                return true;
            }

                else
                {
                    _logger.LogInformation($"Failed to ImportData. The AXIDATA file is not found in the directory");

                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in ImportData.");

                WriteErrorLog($"Failed to Install Package: {packageName}", ex);

                return false;
            }
        }

        private static bool HasAxiDataFile(string downloadDir)
        {
            return Directory.Exists(downloadDir) &&
                   Directory.EnumerateFiles(downloadDir, "*.axidata").Any();
        }

        private async Task<bool> GitPull(string queueData)
        {
            try
            {
                _logger.LogInformation(
                    "GitPull started.");

                //signalrUrl = _configuration["ExportPath:SignalRURL"];

                string apiUrl = _configuration["ApiUrls:axigitpushapi_url"];

                JObject payload =
                    PrepareGitPayload(queueData);

                _logger.LogInformation("GitPull payload \n"+payload);

                ApiResponse response =
                    await _apiService.PostAsync(apiUrl, payload);

                string SRPayload;

                if (!response.Success)
                {
                    _logger.LogError(
                        "GitPull failed. Message : {Message}",
                        response.Message);

                    throw new Exception($"The AXI {packageName} package could not be installed. Please review the installation logs for more details and try again. If the issue persists, contact your administrator.");
                }

                _logger.LogInformation(
                    "GitPull completed successfully.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in GitPull.");

                WriteErrorLog($"Failed to Install Package: {packageName}", ex);

                return false;
            }
        }

        private JObject PrepareStructuresPayload(string queueData)
        {
            JObject queueJson =
        JObject.Parse(queueData);

            JObject importObject = new JObject();

            /*
                axpapp
                from queuedata.appName
            */
            importObject["axpapp"] =
                queueJson["appName"]?.ToString();

            /*
                username
                from queuedata.requestedId
            */
            importObject["username"] =
                queueJson["requestId"]?.ToString();

            /*
                default values
            */
            importObject["trace"] = true;

            importObject["impallfromfolder"] = true;

            //importObject["printdocs"] = true;

            //importObject["compressedfile"] = true;

            /*
                exppath
                from appsettings.json
            */
            //importObject["imppath"] = queueJson["importPath"];//_configuration["ExportPath:exportdir"];
            importObject["imppath"] = downloadDir + "\\" + queueJson["packageName"];

                /*
                    structs
                
                JObject structsObject =
                    PrepareStructs(downloadDir);

            importObject["structs"] =
                structsObject;

            /*
                final payload
            */
            JObject finalPayload = new JObject();

            finalPayload["import"] =
                importObject;

            return finalPayload;
        }

        private JObject PrepareDataPayload(string queueData)
        {
            JObject data = JObject.Parse(queueData);

            data.Remove("actions");
            data.Remove("publish");

            //if (!data.ContainsKey("importdir"))
            //{
            //downloadDir = _configuration["ExportPath:exportdir"];

            data["password"] = "22723bbd4217a0abf6d3e68073c7603d";

            data["importDir"] = downloadDir+"\\"+data["packageName"];

            data["trace"] = "true";
            //}
            //data["exportdir"] = _configuration["ExportPath:exportdir"];
            //data["password"] = "22723bbd4217a0abf6d3e68073c7603d";

            return data;
        }

        private JObject PrepareGitPayload(string queueData)
        {
            JObject data = JObject.Parse(queueData);

            JObject payload = new JObject();

            if (data["imppath"] != null)
            {
                payload["physicalPath"] = data["imppath"];

                downloadDir = payload["physicalPath"].ToString();

            }

            else
            {
                string impD = _configuration["ExportPath:exportdir"];

                if (!string.IsNullOrEmpty(impD))
                {
                    payload["physicalPath"] = impD;
                    downloadDir = impD;
                }

                else { 

                    string getDir = Directory.GetCurrentDirectory();

                    string folderPath = Path.Combine(
                        getDir,
                        data["appName"]?.ToString() ?? "DefaultApp",
                        data["requestedBy"]?.ToString() ?? "DefaultUser",
                        data["packageName"]?.ToString() ?? "DefaultPackage",
                        data["packageVersion"]?.ToString() ?? "1.0"
                    );

                    Directory.CreateDirectory(folderPath);

                    payload["physicalPath"] = folderPath;
                    downloadDir = folderPath;

                    Console.WriteLine("Folder Path -- "+folderPath);
                }
            }
            

            //payload["physicalPath"] = _configuration["ExportPath:exportdir"];

            //payload["targetRepo"] = data["publish"]["repository"];

            payload["packageName"] = data["packageName"];

            payload["gitbranch"] = data["gitbranch"]??"main";

            payload["targetRepo"] = data["repository"];

            //payload["packageDescription"] = data["publish"]["packageDescription"];

            //payload["packageVersion"] = data["packageVersion"];

            //payload["packageAuthor"] = data["requestedBy"]; 

            return payload;
        }

        private JObject PrepareStructs(string folderPath)
        {
            JObject structsObject = new JObject();

            JObject tstructObject = new JObject();

            JObject iviewObject = new JObject();

            JObject pageObject = new JObject();

            /*
                Check folder exists
            */
            if (!Directory.Exists(folderPath))
            {
                throw new Exception(
                    $"Folder does not exist : {folderPath}");
            }

            /*
                Get all .trn files
            */
            string[] trnFiles =
                Directory.GetFiles(
                    folderPath,
                    "*.trn",
                    SearchOption.TopDirectoryOnly);

            /*
                Process .trn files
            */
            foreach (string file in trnFiles)
            {
                /*
                    File name without extension
                */
                string fileName =
                    Path.GetFileNameWithoutExtension(file);

                /*
                    Remove c__ prefix
                */
                if (fileName.StartsWith(
                        "c__",
                        StringComparison.OrdinalIgnoreCase))
                {
                    fileName =
                        fileName.Substring(3);
                }

                /*
                    Add to tstruct
                */
                tstructObject[fileName] =
                    fileName;
            }

            /*
                Get all .ivw files
            */
            string[] ivwFiles =
                Directory.GetFiles(
                    folderPath,
                    "*.ivw",
                    SearchOption.TopDirectoryOnly);

            /*
                Process .ivw files
            */
            foreach (string file in ivwFiles)
            {
                /*
                    File name without extension
                */
                string fileName =
                    Path.GetFileNameWithoutExtension(file);

                /*
                    Remove c__ prefix
                */
                if (fileName.StartsWith(
                        "c__",
                        StringComparison.OrdinalIgnoreCase))
                {
                    fileName =
                        fileName.Substring(3);
                }

                /*
                    Add to iview
                */
                iviewObject[fileName] =
                    fileName;
            }

            string[] pageFiles =
                Directory.GetFiles(
                    folderPath,
                    "*.pge",
                    SearchOption.TopDirectoryOnly);

            foreach (string file in pageFiles)
            {
                /*
                    File name without extension
                */
                string fileName =
                    Path.GetFileNameWithoutExtension(file);

                /*
                    Remove c__ prefix
                */
                if (fileName.StartsWith(
                        "c__",
                        StringComparison.OrdinalIgnoreCase))
                {
                    fileName =
                        fileName.Substring(3);
                }

                /*
                    Add to iview
                */
                pageObject[fileName] =
                    fileName;
            }

            /*
                Final result
            */
            structsObject["tstruct"] =
                tstructObject;

            structsObject["iview"] =
                iviewObject;

            structsObject["iview"] =
                pageObject;

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
                    /*string _srPayload = JsonConvert.SerializeObject(new[]
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
                        });*/

                    return _srPayload;
                }

                return null;
            }

            catch (Exception ex)
            {
                // Call your custom log method here
                //WriteErrorLog("Error in prepareSRPayload: ", ex.ToString());
                _logger.LogError(
                    "Error in prepareSRPaylod ",
                    ex
                    );

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

                _signalR.SendSignalRMessage(signalrUrl, project, clientId, prepSRPayload(title, errMsg));
            }
            catch
            {
                File.AppendAllText("fallback_error.log",
                "Logging failed: " + ex.Message + " | Original: " + title + Environment.NewLine);
                // Avoid throwing exception from logger itself
            }
        }
    }
}