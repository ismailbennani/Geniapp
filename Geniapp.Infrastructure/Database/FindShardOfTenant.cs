using Geniapp.Infrastructure.Database.MasterDatabase;
using Microsoft.EntityFrameworkCore;

namespace Geniapp.Infrastructure.Database;

public class FindShardOfTenant(MasterDbContext masterDbContext)
{
    public async Task<Shard?> FindShard(Guid tenantId)
    {
        TenantShardAssociation? association = await masterDbContext.TenantShardAssociations.Where(s => s.TenantId == tenantId)
            .Include(tenantShardAssociation => tenantShardAssociation.Shard)
            .SingleOrDefaultAsync();
        return association?.Shard;
    }
}
