using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxiInstallConsumerPackage.Models;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AxiInstallConsumerPackage.Services
{
    public class SignalRClass
    {
        private readonly ApiService _apiService;
        private readonly ILogger<SignalRClass> _logger;

        public SignalRClass(ILogger<SignalRClass> logger,
            ApiService apiService)
        {
            _apiService = apiService;
            _logger = logger;
        }
        public async Task SendSignalRMessage(string signalrUrl, string project, string clientId, string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(clientId))
                {
                    var signalRMessage = new
                    {
                        project,
                        UserId = clientId,
                        Message = message
                    };
                  await _apiService.PostAsync(signalrUrl, signalRMessage);
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
