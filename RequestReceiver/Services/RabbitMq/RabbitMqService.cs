using RabbitMQ.Client;

namespace RequestReceiver.Services.RabbitMq
{
    public class RabbitMqService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        public RabbitMqService()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
            _channel.QueueDeclareAsync(queue: "AmoQueue", durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        public IChannel GetChannel() => _channel;

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            GC.SuppressFinalize(this);
        }
    }
}
