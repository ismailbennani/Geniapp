using Geniapp.Infrastructure.Database;

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

    public AgentMasterConfiguration? Master { get; set; }
    public AgentWorkerConfiguration? Worker { get; set; }
    public AgentFrontendConfiguration? Frontend { get; set; }
}
