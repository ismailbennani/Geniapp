using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Geniapp.Infrastructure.MessageQueue;

public class MessageToProcess<TBody>
{
    readonly IModel _channel;
    readonly BasicDeliverEventArgs _args;

    public MessageToProcess(IModel channel, BasicDeliverEventArgs args, TBody body)
    {
        _channel = channel;
        _args = args;
        Body = body;
    }

    public TBody Body { get; }

    public void Ack() => _channel.BasicAck(_args.DeliveryTag, false);
    public void Nack() => _channel.BasicNack(_args.DeliveryTag, false, true);
}
