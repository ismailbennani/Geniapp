namespace Geniapp.Infrastructure.MessageQueue;

public class MessageQueueConfiguration
{
    /// <summary>
    ///     The host that serves the message queue.
    /// </summary>
    public required string HostName { get; init; }
}
