using System.Text;
using AxiInstallConsumerPackage.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AxiInstallConsumerPackage.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;

        public ApiService(
            HttpClient httpClient,
            ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(60);
            _logger = logger;
        }

        public async Task<ApiResponse> PostAsync(
            string url,
            object payload)
        {
            try
            {
                string json =
                    JsonConvert.SerializeObject(payload);

                var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                _logger.LogInformation("Calling API : {Url}", url);


                HttpResponseMessage response =
                    await _httpClient.PostAsync(url, content);

                string responseContent = await response.Content.ReadAsStringAsync();

                if (url.Contains("AxInstallerRest.dll"))
                {
                    JObject responseJson = JObject.Parse(responseContent);

                    _logger.LogInformation("Response JSON:\n{Json}", json);

                    string result = responseJson["result"]?.ToString();
                    string command = responseJson["command"]?.ToString();
                    string message = responseJson["message"]?.ToString();

                    _logger.LogInformation(
                        "Result : {Result}, Command : {Command}, Message : {Message}",
                        result,
                        responseJson["command"].ToString(),
                        responseJson["message"].ToString());

                    if (!string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError(
                            "API business failure. Url : {Url}, Command : {Command}, Message : {Message}",
                            url,
                            command,
                            message);

                        return new ApiResponse
                        {
                            Success = false,
                            StatusCode = (int)response.StatusCode,
                            Message = message ?? "API returned failed status.",
                            ResponseContent = responseContent
                        };
                    }

                    _logger.LogInformation(
                        "API success. Url : {Url}, Command : {Command}",
                        url,
                        command);

                    return new ApiResponse
                    {
                        Success = true,
                        StatusCode = (int)response.StatusCode,
                        Message = message ?? "API call successful.",
                        ResponseContent = responseContent
                    };
                }

                else if(url.Contains("SendSignalR"))
                {
                    if (responseContent.Equals("SUCCESS"))
                    {
                        _logger.LogInformation(
                       "API success. Url : {Url}",
                       url);

                        return new ApiResponse
                        {
                            Success = true,
                            StatusCode = (int)response.StatusCode,
                            Message = "API call success.",
                            ResponseContent = responseContent
                        };
                    }

                    _logger.LogError(
                            "SendSignalR failure. Url : {Url}, Response : {Response}",
                            url,
                            responseContent);

                    return new ApiResponse
                    {
                        Success = false,
                        StatusCode = (int)response.StatusCode,
                        Message = "SendSignalR failed status.",
                        ResponseContent = responseContent
                    };
                }

                else
                {
                    JObject responseJson = JObject.Parse(responseContent);

                    bool success = responseJson["success"] != null ? (bool)responseJson["success"] : (bool)responseJson["Success"];


                    if (success)
                    {
                        _logger.LogInformation(
                        "API Business success. Url : {Url}",
                        url);

                        return new ApiResponse
                        {
                            Success = true,
                            StatusCode = (int)response.StatusCode,
                            Message = "API call success.",
                            ResponseContent = responseContent
                        };
                    }

                    _logger.LogError(
                            "API business failure. Url : {Url}, Response : {Response}",
                            url,
                            responseContent);

                    return new ApiResponse
                    {
                        Success = false,
                        StatusCode = (int)response.StatusCode,
                        Message = "API returned failed status.",
                        ResponseContent = responseContent
                    };

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception while calling API : {Url}",
                    url);

                return new ApiResponse
                {
                    Success = false,
                    StatusCode = 500,
                    Message = ex.Message,
                    ResponseContent = string.Empty
                };
            }
        }
    }
}