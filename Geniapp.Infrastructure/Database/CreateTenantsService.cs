using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.Database.ShardDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Geniapp.Infrastructure.Database;

public class CreateTenantsService(
    MasterDbContext masterDbContext,
    ShardContextProvider shardContextProvider,
    ShardConfigurations configurations,
    ILogger<CreateTenantsService> logger
)
{
    public async Task<Tenant> CreateTenantAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ShardConfiguration> shards = configurations.GetAllShardConfigurations();
        ShardConfiguration? randomShard = shards.Skip(Random.Shared.Next(shards.Count)).FirstOrDefault();
        if (randomShard == null)
        {
            throw new InvalidOperationException("No shard configured");
        }

        Tenant tenant = await CreateTenantInShardAsync(randomShard, cancellationToken);

        try
        {
            await CreateTenantAssociationAsync(randomShard, tenant, cancellationToken);
        }
        catch
        {
            logger.LogInformation("Removing tenant {TenantId} because it could not be registered in the master database...", tenant.Id);
            await RemoveTenantInShardAsync(randomShard, tenant, cancellationToken);
            throw;
        }

        logger.LogInformation("Successfully created tenant {TenantId} in shard {ShardName}.", tenant.Id, randomShard.Name);

        return tenant;
    }

    async Task<Tenant> CreateTenantInShardAsync(ShardConfiguration shard, CancellationToken cancellationToken = default)
    {
        ShardDbContext? shardContext = await shardContextProvider.GetShardContextAsync(shard.Name);
        if (shardContext == null)
        {
            throw new InvalidOperationException($"Could not create context for shard {shard.Name}.");
        }

        await using IDbContextTransaction transaction = await shardContext.Database.BeginTransactionAsync(cancellationToken);

        Tenant newTenant = new();
        shardContext.Tenants.Add(newTenant);

        TenantData tenantData = new(newTenant);
        shardContext.TenantsData.Add(tenantData);

        await shardContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return newTenant;
    }

    async Task<TenantShardAssociation> CreateTenantAssociationAsync(ShardConfiguration shard, Tenant tenant, CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction transaction = await masterDbContext.Database.BeginTransactionAsync(cancellationToken);

        Shard? shardEntity = await masterDbContext.Shards.FirstOrDefaultAsync(s => s.Name == shard.Name, cancellationToken);
        if (shardEntity == null)
        {
            throw new InvalidOperationException($"Shard {shard.Name} has not been registered in master database.");
        }

        TenantShardAssociation newAssociation = new(tenant.Id, shardEntity);
        masterDbContext.TenantShardAssociations.Add(newAssociation);

        await masterDbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return newAssociation;
    }

    async Task RemoveTenantInShardAsync(ShardConfiguration shard, Tenant tenant, CancellationToken cancellationToken = default)
    {
        ShardDbContext? shardContext = await shardContextProvider.GetShardContextAsync(shard.Name);
        if (shardContext == null)
        {
            throw new InvalidOperationException($"Could not create context for shard {shard.Name}.");
        }

        await using IDbContextTransaction transaction = await shardContext.Database.BeginTransactionAsync(cancellationToken);

        TenantData? tenantData = await shardContext.TenantsData.SingleOrDefaultAsync(t => t.Tenant.Id == tenant.Id, cancellationToken);
        if (tenantData != null)
        {
            shardContext.Remove(tenantData);
        }

        Tenant? tenantEntity = await shardContext.Tenants.SingleOrDefaultAsync(t => t.Id == tenant.Id, cancellationToken);
        if (tenantEntity != null)
        {
            shardContext.Remove(tenantEntity);
        }

        await shardContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
