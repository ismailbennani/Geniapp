using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.Database.ShardDatabase;
using Microsoft.EntityFrameworkCore;

namespace Geniapp.Infrastructure.Database;

public class ShardContextProvider(FindShardOfTenant findShardOfTenant, ShardConfigurations shardConfigurations)
{
    public async Task<ShardDbContext?> GetShardContext(Guid tenantId)
    {
        Shard? shard = await findShardOfTenant.FindShard(tenantId);
        if (shard == null)
        {
            return null;
        }

        ShardConfiguration? configuration = shardConfigurations.GetShardConfiguration(shard.Name);
        if (configuration == null)
        {
            return null;
        }

        return new ShardDbContext(configuration.ConnectionString, new DbContextOptions<ShardDbContext>());
    }
}
