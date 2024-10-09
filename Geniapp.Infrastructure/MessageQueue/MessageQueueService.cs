using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Geniapp.Infrastructure.MessageQueue;

public class MessageQueueService(IOptions<MessageQueueConfiguration> configuration, ILogger<MessageQueueService> logger) : IDisposable
{
    readonly ConnectionFactory _factory = new() { HostName = configuration.Value.HostName };
    IConnection? _connection;
    IModel? _channel;
    readonly HashSet<string> _declaredQueues = [];
    readonly Dictionary<string, EventingBasicConsumer> _consumers = [];

    public MessageQueueInformation GetQueueInformation(string queueName) => MessageQueueInformation.Get(GetChannel(), queueName);

    public void PublishMessage(string queueName, byte[] message)
    {
        IModel channel = GetChannel();

        if (!_declaredQueues.Contains(queueName))
        {
            channel.QueueDeclare(queueName, false, false, false, null);
            _declaredQueues.Add(queueName);
        }

        channel.BasicPublish(string.Empty, queueName, null, message);
        logger.LogDebug("Published message of length {Length} in queue {Queue}.", message.Length, queueName);
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
            logger.LogInformation("Started consuming messages from queue {Queue}.", queueName);

            registeredConsumer = new EventingBasicConsumer(channel);
            registeredConsumer.Received += (_, args) => logger.LogDebug("Received message of length {Length} in queue {Queue}.", args.Body.Length, queueName);
            channel.BasicConsume(queueName, false, registeredConsumer);
            _consumers[queueName] = registeredConsumer;
        }

        registeredConsumer.Received += (_, args) => consumer(channel, args);
    }

    IModel GetChannel() => _channel ??= GetConnection().CreateModel();
    IConnection GetConnection() => _connection ??= _factory.CreateConnection();

    public void Dispose()
    {
        _connection?.Dispose();
        _channel?.Dispose();

        GC.SuppressFinalize(this);
    }
}
