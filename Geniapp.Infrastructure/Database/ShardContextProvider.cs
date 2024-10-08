using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.Database.ShardDatabase;

namespace Geniapp.Infrastructure.Database;

public class ShardContextProvider(FindShardOfTenant findShardOfTenant, ShardConfigurations shardConfigurations)
{
    public async Task<ShardDbContext?> GetShardContext(string shardName)
    {
        ShardConfiguration? configuration = shardConfigurations.GetShardConfiguration(shardName);
        if (configuration == null)
        {
            return null;
        }

        return new ShardDbContext(configuration.ConnectionString);
    }

    public async Task<ShardDbContext?> GetShardContextOfTenant(Guid tenantId)
    {
        Shard? shard = await findShardOfTenant.FindShard(tenantId);
        if (shard == null)
        {
            return null;
        }

        return await GetShardContext(shard.Name);
    }
}
