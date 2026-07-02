using System.Text;
using AxiExportPackage.Helpers;
using AxiExportPackage.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AxiExportPackage.Consumers
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
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            string queueName =
                _configuration["RabbitMQ:QueueName"];

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
            string queueName =
                _configuration["RabbitMQ:QueueName"];

            var consumer =
                new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                string message = string.Empty;

                try
                {
                    byte[] body = ea.Body.ToArray();

                    message =
                        Encoding.UTF8.GetString(body);

                    _logger.LogInformation(
                        "Message received from queue.\n");

                    _logger.LogInformation(
                        message);

                    string queueData =
                        QueueDataHelper.ExtractQueueData(message);

                    using IServiceScope scope =
                        _scopeFactory.CreateScope();

                    var exportService =
                        scope.ServiceProvider
                        .GetRequiredService<IPackageExportService>();

                    await exportService
                        .ProcessPackageExport(queueData);

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