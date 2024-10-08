using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.MessageQueue;

namespace Geniapp.Infrastructure;

public class SharedConfiguration
{
    /// <summary>
    ///     The connection strings to the master database.
    /// </summary>
    public required string MasterConnectionString { get; set; }

    /// <summary>
    ///     The connection strings to the shards
    /// </summary>
    public ShardConfiguration[] Shards { get; set; } = [];

    /// <summary>
    ///     The message queue to use.
    /// </summary>
    public MessageQueueConfiguration MessageQueue { get; set; } = new();
}
