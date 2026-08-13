using AxiInstallConsumerPackage.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AxiInstallConsumerPackage.Services
{
    public class RedisService
    {
        private readonly ApiService _apiService;
        private readonly ILogger<RedisService> _logger;

        private static string _redisDelUrl;
        private static string _redisWriteUrl;
        private readonly string armBaseUrl;

        public RedisService(IConfiguration configuration,ApiService apiService, ILogger<RedisService> logger)
        {
            armBaseUrl = configuration["armbaseurl"]
                ?? throw new ArgumentNullException("armbaseurl is missing in appsettings.json");

            _redisWriteUrl = $"{armBaseUrl}/AxUtils/api/v1/AxRedisWrite";
            _redisDelUrl = $"{armBaseUrl}/AxUtils/api/v1/AxRedisDelete";

            _apiService = apiService;
            _logger = logger;
        }

        public async Task DeleteKey(string key, string project)
        {
            key = key.Replace(" ", "_");

            JObject deleteJson = PrepDeleteRedis(project, key);

            _logger.LogInformation($"RedisKey {deleteJson}");

            ApiResponse response = await _apiService.PostAsync(_redisDelUrl, deleteJson);

            if (!response.Success)
            {
                _logger.LogInformation("\nAxDeleteRedis API call failed");
            }

            await Task.CompletedTask;
        }

        public async Task SetRedisKey(string key, string value, string mins, string project)
        {
            key = key.Replace(" ", "_");

            JObject inprogressJson = PrepWriteRedis(project, key, value, mins);

            _logger.LogInformation($"RedisKey {inprogressJson}");

            ApiResponse response = await _apiService.PostAsync(_redisWriteUrl, inprogressJson);

            if(!response.Success)
            {
                _logger.LogInformation("\nAxWriteRedis API call failed");
            }

            await Task.CompletedTask;
        }

        private JObject PrepWriteRedis(string project, string key, string Value, string mins)
        {
            JObject WriteRedis = new JObject();

            WriteRedis["AccessCode"] = project;

            WriteRedis["InMemoryKey"] = key;

            WriteRedis["InMemoryValue"] = Value;

            WriteRedis["IsBinary"] = "false";

            WriteRedis["KeyExpiryInMins"] = mins;

            return WriteRedis;
        }

        private JObject PrepDeleteRedis(string project, string key)
        {
            JObject DeleteRedis = new JObject();

            DeleteRedis["AccessCode"] = project;

            DeleteRedis["InMemoryKey"] = key;

            return DeleteRedis;
        }
    }
}