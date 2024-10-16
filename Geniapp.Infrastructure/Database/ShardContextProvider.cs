﻿using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.Database.ShardDatabase;

namespace Geniapp.Infrastructure.Database;

public class ShardContextProvider(FindShardOfTenant findShardOfTenant, ShardConfigurations shardConfigurations)
{
    public Task<ShardDbContext?> GetShardContextAsync(string shardName)
    {
        ShardConfiguration? configuration = shardConfigurations.GetShardConfiguration(shardName);
        if (configuration == null)
        {
            return Task.FromResult<ShardDbContext?>(null);
        }

        return Task.FromResult<ShardDbContext?>(new ShardDbContext(configuration.ConnectionString));
    }

    public async Task<ShardDbContext?> GetShardContextOfTenant(Guid tenantId)
    {
        Shard? shard = await findShardOfTenant.FindShard(tenantId);
        if (shard == null)
        {
            return null;
        }

        return await GetShardContextAsync(shard.Name);
    }
}
