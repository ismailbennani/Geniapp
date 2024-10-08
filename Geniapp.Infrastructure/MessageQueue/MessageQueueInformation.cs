using RabbitMQ.Client;

namespace Geniapp.Infrastructure.MessageQueue;

public class MessageQueueInformation
{
    MessageQueueInformation(uint consumerCount, uint messageCount)
    {
        ConsumerCount = consumerCount;
        MessageCount = messageCount;
    }

    public uint ConsumerCount { get; }
    public uint MessageCount { get; }

    public static MessageQueueInformation Get(IModel channel, string queueName) => new(channel.ConsumerCount(queueName), channel.MessageCount(queueName));
}
