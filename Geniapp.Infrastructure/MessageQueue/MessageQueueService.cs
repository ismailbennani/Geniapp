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
    readonly Dictionary<string, EventingBasicConsumer> _consumers = [];

    public MessageQueueInformation GetQueueInformation(string queueName) => MessageQueueInformation.Get(GetChannel(), queueName);

    /// <summary>
    ///     Queue in the Work Queues use case: https://www.rabbitmq.com/tutorials/tutorial-two-dotnet
    /// </summary>
    public void DeclareWorkQueue(string queueName)
    {
        IModel channel = GetChannel();
        channel.QueueDeclare(queueName, false, false, false, null);
    }

    /// <summary>
    ///     Emitter in the Work Queues use case: https://www.rabbitmq.com/tutorials/tutorial-two-dotnet
    /// </summary>
    public void PublishWork(string queueName, byte[] message)
    {
        IModel channel = GetChannel();

        channel.BasicPublish(string.Empty, queueName, null, message);
        logger.LogDebug("Published message of length {Length} in queue {Queue}.", message.Length, queueName);
    }

    /// <summary>
    ///     Receiver in the Work Queues use case: https://www.rabbitmq.com/tutorials/tutorial-two-dotnet
    /// </summary>
    public void RegisterWorker(string queueName, Action<IModel, BasicDeliverEventArgs> consumer)
    {
        IModel channel = GetChannel();
        //channel.BasicQos(0, 2, false);

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

    /// <summary>
    ///     Exchange in the Publish/Subscribe use case: https://www.rabbitmq.com/tutorials/tutorial-three-dotnet
    /// </summary>
    public void DeclareBroadcast(string exchangeName)
    {
        IModel channel = GetChannel();
        channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout);
    }

    /// <summary>
    ///     Emitter in the Publish/Subscribe use case: https://www.rabbitmq.com/tutorials/tutorial-three-dotnet
    /// </summary>
    public void BroadcastMessage(string exchangeName, byte[] message)
    {
        IModel channel = GetChannel();
        channel.BasicPublish(exchangeName, string.Empty, null, message);
    }

    /// <summary>
    ///     Receiver in the Publish/Subscribe use case: https://www.rabbitmq.com/tutorials/tutorial-three-dotnet
    /// </summary>
    public void RegisterSubscriber(string exchangeName, Action<IModel, BasicDeliverEventArgs> consumer)
    {
        IModel channel = GetChannel();

        string? queueName = channel.QueueDeclare().QueueName;
        channel.QueueBind(queueName, exchangeName, string.Empty);

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
