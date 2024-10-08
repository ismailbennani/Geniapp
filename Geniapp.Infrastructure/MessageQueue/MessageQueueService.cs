using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Geniapp.Infrastructure.MessageQueue;

public class MessageQueueService : IDisposable
{
    readonly ILogger<MessageQueueService> _logger;

    readonly ConnectionFactory _factory;
    IConnection? _connection;
    IModel? _channel;
    readonly HashSet<string> _declaredQueues = [];
    readonly Dictionary<string, EventingBasicConsumer> _consumers = [];
    readonly HashSet<string> _consumedQueues = [];

    public MessageQueueService(MessageQueueConfiguration configuration, ILogger<MessageQueueService> logger)
    {
        _logger = logger;
        _factory = new ConnectionFactory { HostName = configuration.HostName };
    }

    public void PublishMessage(string queueName, byte[] message)
    {
        IModel channel = GetChannel();

        if (!_declaredQueues.Contains(queueName))
        {
            channel.QueueDeclare(queueName, false, false, false, null);
            _declaredQueues.Add(queueName);
        }

        channel.BasicPublish(string.Empty, queueName, null, message);
        _logger.LogDebug("Published message of length {Length} in queue {Queue}.", message.Length, queueName);
    }

    public void RegisterConsumer(string queueName, Action<IModel, BasicDeliverEventArgs> consumer)
    {
        IModel channel = GetChannel();

        if (!_declaredQueues.Contains(queueName))
        {
            channel.QueueDeclare(queueName, false, false, false, null);
            channel.BasicQos(0, 2, false);
            _declaredQueues.Add(queueName);
        }

        if (!_consumers.TryGetValue(queueName, out EventingBasicConsumer? registeredConsumer))
        {
            _logger.LogInformation("Started consuming messages from queue {Queue}.", queueName);

            registeredConsumer = new EventingBasicConsumer(channel);
            registeredConsumer.Received += (_, args) => _logger.LogDebug("Received message of length {Length} in queue {Queue}.", args.Body.Length, queueName);
            channel.BasicConsume(queueName, false, registeredConsumer);
            _consumers[queueName] = registeredConsumer;
        }

        registeredConsumer.Received += (_, args) => consumer(channel, args);
    }

    public void Reconnect()
    {
        _connection?.Dispose();
        _channel?.Dispose();
        _declaredQueues.Clear();
        _consumedQueues.Clear();
    }

    IConnection GetConnection() => _connection ??= _factory.CreateConnection();

    IModel GetChannel()
    {
        if (_channel == null)
        {
            _channel = GetConnection().CreateModel();
            _channel.ConfirmSelect();
        }
        return _channel;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _channel?.Dispose();
    }
}
