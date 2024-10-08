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

    public void RegisterConsumer(string queueName, Action<BasicDeliverEventArgs> consumer)
    {
        IModel channel = GetChannel();

        if (!_declaredQueues.Contains(queueName))
        {
            channel.QueueDeclare(queueName, false, false, false, null);
            _declaredQueues.Add(queueName);
        }

        if (!_consumedQueues.Contains(queueName))
        {
            _logger.LogInformation("Started consuming messages from queue {Queue}.", queueName);

            EventingBasicConsumer logConsumer = new(channel);
            logConsumer.Received += (_, args) => _logger.LogDebug("Received message of length {Length} in queue {Queue}.", args.Body.Length, queueName);
            channel.BasicConsume(queueName, true, logConsumer);
            _consumedQueues.Add(queueName);
        }

        EventingBasicConsumer basicConsumer = new(channel);
        basicConsumer.Received += (_, args) => consumer(args);
        channel.BasicConsume(queueName, true, basicConsumer);
    }

    public void Reconnect()
    {
        _connection?.Dispose();
        _channel?.Dispose();
        _declaredQueues.Clear();
        _consumedQueues.Clear();
    }

    IConnection GetConnection() => _connection ??= _factory.CreateConnection();
    IModel GetChannel() => _channel ??= GetConnection().CreateModel();

    public void Dispose()
    {
        _connection?.Dispose();
        _channel?.Dispose();
    }
}
