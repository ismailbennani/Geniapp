﻿using Geniapp.Infrastructure.Database;

namespace Geniapp.Master;

public class MasterConfiguration
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
    ///     The port to use for the admin API server.
    /// </summary>
    public int Port { get; set; }
}
