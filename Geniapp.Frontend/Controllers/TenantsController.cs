using Geniapp.Frontend.Models;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.Database.ShardDatabase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Geniapp.Frontend.Controllers;

[Route("/tenants")]
[ApiController]
public class TenantsController(MasterDbContext masterDbContext, ShardContextProvider shardContextProvider, ILogger<TenantsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<TenantDto>> GetTenantsData()
    {
        Shard[] shards = await masterDbContext.Shards.ToArrayAsync();
        List<TenantDto> result = [];

        foreach (Shard shard in shards)
        {
            ShardDbContext? context = await shardContextProvider.GetShardContext(shard.Name);
            if (context == null)
            {
                logger.LogWarning("Could not build context for shard {ShardName}.", shard.Name);
                continue;
            }

            await using ShardDbContext _ = context;
            TenantData[] tenantData = await context.TenantsData.Include(tenantData => tenantData.Tenant).ToArrayAsync();

            foreach (TenantData data in tenantData)
            {
                result.Add(
                    new TenantDto
                    {
                        TenantId = data.Tenant.Id,
                        ShardId = shard.Id,
                        LastWorkerId = data.LastWorkerId,
                        LastModificationDate = data.LastModificationDate
                    }
                );
            }
        }

        return result;
    }
}
