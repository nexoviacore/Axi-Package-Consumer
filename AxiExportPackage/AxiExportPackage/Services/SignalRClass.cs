using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AxiExportPackage.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace AxiExportPackage.Services
{
    public class SignalRClass
    {
        private readonly ApiService _apiService;
        private readonly ILogger<SignalRClass> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private NpgsqlConnection? _connection;

        private string connectString;

        public SignalRClass(ILogger<SignalRClass> logger,
            IConfiguration configuration,
            ApiService apiService,
            HttpClient httpClient)
        {
            _apiService = apiService;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;

            connectString = _configuration["ConnectionStrings:PostgresDB"];
        }
        public async Task SendSignalRMessage(string signalrUrl, string projectName, string clientId, string message, JObject activepayload, JObject exporttablepayload)
        {
            try
            {
                if (!string.IsNullOrEmpty(clientId))
                {
                    //message = JsonConvert.DeserializeObject<string>(message);

                    //InsertData(activepayload);

                    var signalRMessage = new
                    {
                        project = projectName,
                        UserId = clientId,
                        Message = message
                    };
                    //await _apiService.PostAsync(signalrUrl, JsonConvert.SerializeObject(signalRMessage));

                    await _apiService.PostAsync(signalrUrl, signalRMessage,"SignalR");

                    if (CheckConnection())
                    {
                        InsertData(activepayload);
                        InsertAxiProduct(exporttablepayload);
                    }

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

        public void InsertData(JObject axactive)
        {
            try {
                string productName = axactive["productName"].ToString();
                string status = axactive["status"].ToString();
                string username = axactive["username"].ToString();

                string taskid = GetTaskId();

                string query = $@"INSERT INTO axactivemessages(eventdatetime, msgtype, fromuser, touser, taskid, tasktype, processname, 
taskname, indexno, keyfield, keyvalue, transid, displaytitle, displaycontent, effectivefrom, 
effectiveto, html, scripts, hide, displayicon, hlink, hlink_transid, hlink_params, requestpayload)
VALUES(@timeStamp, 'Export Product', @username, @username, @Taskid, 'Product', 'Export Product',
'Export Product', 0, NULL, NULL, '', 'Status of export product '|| @productName, @status, NULL, 
NULL, NULL, NULL, NULL, NULL, NULL, NULL, '', '');";




                //using var connection = new NpgsqlConnection(connectString);
                using var command = new NpgsqlCommand(query, _connection);

                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@productName", productName);
                command.Parameters.AddWithValue("@Taskid", taskid);
                command.Parameters.AddWithValue("@timeStamp", DateTime.Now.ToString("yyyyMMddHHmmssfff"));

                //connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                    _logger.LogInformation("Record inserted to AxActiveMessages successfully");

                else
                    _logger.LogError("Insertion to AxActiveMessages failed. Please check the connections and axactivepayload");
            }

            catch(Exception ex)
            {
                _logger.LogError("Exception in InsertData to AxActiveMessages : {ex}",ex);
            }
        }

        public bool CheckConnection()
        {
            try
            {
                _connection = new NpgsqlConnection(connectString);

                if (_connection == null)
                    return false;

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public String GetTaskId()
        {

            const string query = "SELECT fn_axdbput_getrecid(1, 1)";

            using var command = new NpgsqlCommand(query, _connection);

            object? result = command.ExecuteScalar();

            return result?.ToString() ?? string.Empty;
        }

        public void InsertAxiProduct(JObject axiExportPayload)
        {
            string query = "UPDATE axi_export_products SET status = @status, remarks = @remarks WHERE package_name= @packagename  AND version = 'V' || @version;";

            string status = axiExportPayload["status"].ToString();
            string remarks = axiExportPayload["remarks"].ToString(); ;
            string packageName = axiExportPayload["packageName"].ToString(); ;
            string version = axiExportPayload["version"].ToString();

            using var command = new NpgsqlCommand(query, _connection);

            command.Parameters.AddWithValue("@remarks", remarks);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@version", version);
            command.Parameters.AddWithValue("@packagename", packageName);

            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected > 0)
                _logger.LogInformation("Record inserted to axi_export_product successfully");

            else
                _logger.LogError("Insertion to axi_export_product failed. Please check the connections and axi_export payload");
        }
    }
}