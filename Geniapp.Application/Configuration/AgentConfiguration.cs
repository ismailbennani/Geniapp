using Geniapp.Master;

namespace Geniapp.Application.Configuration;

class AgentConfiguration
{
    /// <summary>
    ///     The connection strings to the master database.
    /// </summary>
    public required string MasterConnectionString { get; set; }

    /// <summary>
    ///     The connection strings to the shards
    /// </summary>
    public ShardConfiguration[] Shards { get; set; } = [];

    public MasterConfiguration? Master { get; set; }
    public WorkerConfiguration? Worker { get; set; }
    public FrontendConfiguration? Frontend { get; set; }
}
