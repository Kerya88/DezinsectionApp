using DezinsectionApp.Services.AmoCrm.Lead;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DezinsectionApp.BackgroundServices
{
    public class RabbitMqBackgroundService : BackgroundService
    {
        private readonly IAmoCrmLeadService _amoCrmLeadService;
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        public RabbitMqBackgroundService(IAmoCrmLeadService amoCrmLeadService)
        {
            _amoCrmLeadService = amoCrmLeadService;

            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
            _channel.QueueDeclareAsync(queue: "AmoQueue", durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                StartListening();

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }

        private void StartListening()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageBody = Encoding.UTF8.GetString(body);

                // Десериализация сообщения
                var message = JsonSerializer.Deserialize<Message>(messageBody);

                if (message != null)
                {
                    switch (message.EndpointType)
                    {
                        case "post":
                            _ = Task.Run(async () => await _amoCrmLeadService.AcceptLead(message.Body));
                            break;
                        case "assignmaster":
                            _ = Task.Run(async () => await _amoCrmLeadService.AssignMaster(message.Body));
                            break;
                        case "notify":
                            _ = Task.Run(async () => await _amoCrmLeadService.NotifyMaster(message.Body));
                            break;
                        default:
                            break;
                    }
                }

                // Подтверждаем обработку сообщения
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            _channel.BasicConsumeAsync("AmoQueue", false, consumer);
        }

        private class Message
        {
            public string EndpointType { get; set; }
            public string Body { get; set; }
        }
    }
}
