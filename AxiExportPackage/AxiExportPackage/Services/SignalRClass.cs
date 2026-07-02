using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxiExportPackage.Models;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AxiExportPackage.Services
{
    public class SignalRClass
    {
        private readonly ApiService _apiService;
        private readonly ILogger<SignalRClass> _logger;
        private readonly HttpClient _httpClient;

        public SignalRClass(ILogger<SignalRClass> logger,
            ApiService apiService,
            HttpClient httpClient)
        {
            _apiService = apiService;
            _logger = logger;
            _httpClient = httpClient;
        }
        public async Task SendSignalRMessage(string signalrUrl, string project, string clientId, string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(clientId))
                {
                    //message = JsonConvert.DeserializeObject<string>(message);

                    var signalRMessage = new
                    {
                        project = project,
                        UserId = clientId,
                        Message = message
                    };
                    //await _apiService.PostAsync(signalrUrl, JsonConvert.SerializeObject(signalRMessage));

                    await _apiService.PostAsync(signalrUrl, signalRMessage,"SignalR");

                }
            }

            catch (Exception ex)
            {
                _logger.LogError(
                     ex,
                     "Exception in signalR : {ex}",
                     ex);
            }
        }
    }
}
