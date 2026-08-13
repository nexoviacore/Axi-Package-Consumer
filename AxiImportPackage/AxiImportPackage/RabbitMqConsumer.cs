using System.Text;
using AxiInstallConsumerPackage.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AxiInstallConsumerPackage
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private IConnection _connection;
        private IModel _channel;

        public RabbitMqConsumer(
            IConfiguration configuration,
            ILogger<RabbitMqConsumer> logger,
            IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;

            InitializeRabbitMq();
        }

        private void InitializeRabbitMq()
        {
            if (string.IsNullOrWhiteSpace(_configuration.GetSection("RabbitMQ").ToString()))
            {
                Console.WriteLine("RabbitMQ configuration is missing or empty. Please define proper RMQ Configurations in appsettings.json");
                return;
            }

            string host = _configuration["RabbitMQ:Host"];
            string userName = _configuration["RabbitMQ:UserName"];
            string password = _configuration["RabbitMQ:Password"];
            string portValue = _configuration["RabbitMQ:Port"];

            string queueName = _configuration["RabbitMQ:QueueName"];

            if (string.IsNullOrWhiteSpace(host))
                throw new Exception("RabbitMQ:Host is missing or empty in appsettings.json");

            if (string.IsNullOrWhiteSpace(userName))
                throw new Exception("RabbitMQ:UserName is missing or empty in appsettings.json");

            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("RabbitMQ:Password is missing or empty in appsettings.json");

            if (string.IsNullOrWhiteSpace(portValue))
                throw new Exception("RabbitMQ:Port is missing or empty in appsettings.json");

            if (string.IsNullOrWhiteSpace(queueName))
                throw new Exception("RabbitMQ:QueueName is missing or empty in appsettings.json");

            if (!int.TryParse(portValue, out int port))
                throw new Exception("RabbitMQ:Port is not a valid integer");

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"],
                Port = Convert.ToInt32(_configuration["RabbitMQ:Port"]),
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        protected override Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            string queueName = _configuration["RabbitMQ:QueueName"];


            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                string message = string.Empty;

                try
                {
                    byte[] body = ea.Body.ToArray();

                    message = Encoding.UTF8.GetString(body);

                    
                    _logger.LogInformation(
                        "Message received from queue.");

                    _logger.LogInformation("Payload "+message);

                    string queueData =
                        QueueDataHelper.ExtractQueueData(message);

                    using IServiceScope scope =
                        _scopeFactory.CreateScope();

                    var exportService =
                        scope.ServiceProvider
                        .GetRequiredService<IPackageImportService>();

                    await exportService.ProcessPackageImport(queueData);

                    _channel.BasicAck(
                        ea.DeliveryTag,
                        false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing RMQ message.");

                    _channel.BasicNack(
                        ea.DeliveryTag,
                        false,
                        false);
                }
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation(
                "RabbitMQ consumer started.");

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();

            base.Dispose();
        }
    }
}