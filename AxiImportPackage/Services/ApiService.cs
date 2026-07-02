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

                _logger.LogInformation(
                    "Calling API : {Url}",
                    url);

                HttpResponseMessage response =
                    await _httpClient.PostAsync(url, content);

                string responseContent =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "API failed. Url : {Url}, StatusCode : {StatusCode}, Response : {Response}",
                        url,
                        response.StatusCode,
                        responseContent);

                    return new ApiResponse
                    {
                        Success = false,
                        StatusCode = (int)response.StatusCode,
                        Message = "API call failed.",
                        ResponseContent = responseContent
                    };
                }

                if (url.Contains("ImportStructs")) 
                {
                    JObject responseJson = JObject.Parse(responseContent);

                    string status = responseJson["result"]?[0]?["status"]
                        ?.ToString();

                    if (!string.Equals(
                            status,
                            "success",
                            StringComparison.OrdinalIgnoreCase))
                    {
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
                    JObject responseJson =
                    JObject.Parse(responseContent);

                    bool success = (bool)responseJson["success"];
                    string message = (string)responseJson["message"];

                    if (success)
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

                /*JObject responseJson =
                    JObject.Parse(responseContent);

                string status =
                    responseJson["result"]?[0]?["status"]
                    ?.ToString();

                if (!string.Equals(
                        status,
                        "success",
                        StringComparison.OrdinalIgnoreCase))
                {
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

                _logger.LogInformation(
                    "API success. Url : {Url}",
                    url);

                return new ApiResponse
                {
                    Success = true,
                    StatusCode = (int)response.StatusCode,
                    Message = "API call success.",
                    ResponseContent = responseContent
                };*/
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