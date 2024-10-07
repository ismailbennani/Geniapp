using Geniapp.Infrastructure.Database;

namespace Geniapp.Worker;

public class WorkerConfiguration
{
    /// <summary>
    ///     The connection strings to the master database.
    /// </summary>
    public required string MasterConnectionString { get; set; }

    /// <summary>
    ///     The connection strings to the shards
    /// </summary>
    public ShardConfiguration[] Shards { get; set; } = [];
}
