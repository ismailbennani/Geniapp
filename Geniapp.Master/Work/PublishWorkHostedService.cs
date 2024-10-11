using System.Collections.Concurrent;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.Database.ShardDatabase;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Infrastructure.Work;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geniapp.Master.Work;

public class PublishWorkHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<PublishWorkConfiguration> configuration,
    ILogger<PublishWorkHostedService> logger
) : BackgroundService
{
    readonly ConcurrentDictionary<Guid, HashSet<Guid>> _alreadySeenInShard = [];
    DateTime _lastTriggerTime = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(configuration.Value.DelayInSeconds), stoppingToken);

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                logger.LogInformation("=============== Start publishing work...");
                await PublishWorkAsync();
                logger.LogInformation("=============== Done publishing work.");
            }
            catch (Exception exn)
            {
                logger.LogError(exn, "An unexpected exception occurred while publishing new work.");
            }
        }
    }

    async Task PublishWorkAsync()
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        await using MasterDbContext masterDbContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        Shard[] shards = masterDbContext.Shards.AsNoTracking().ToArray();
        await Task.WhenAll(shards.Select(PublishWorkOfShard));

        _lastTriggerTime = DateTime.Now;
    }

    async Task PublishWorkOfShard(Shard shard)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ShardContextProvider shardContextProvider = scope.ServiceProvider.GetRequiredService<ShardContextProvider>();
        MessageQueueAdapter adapter = scope.ServiceProvider.GetRequiredService<MessageQueueAdapter>();

        ShardDbContext? context = await shardContextProvider.GetShardContextAsync(shard.Name);
        if (context == null)
        {
            logger.LogWarning("Could not create context for shard {Shard}.", shard.Name);
            return;
        }

        HashSet<Guid> alreadySeen = _alreadySeenInShard.GetOrAdd(shard.Id, []);

        await using ShardDbContext _ = context;
        TenantData[] tenants = await context.TenantsData.AsNoTracking().Include(d => d.Tenant).ToArrayAsync();
        Random.Shared.Shuffle(tenants);

        foreach (TenantData tenantData in tenants)
        {
            if (!alreadySeen.Contains(tenantData.Tenant.Id) || tenantData.LastModificationDate > _lastTriggerTime)
            {
                WorkItem workItem = new() { TenantId = tenantData.Tenant.Id };
                adapter.PublishWork(workItem);

                alreadySeen.Add(tenantData.Tenant.Id);
            }

            await Task.Yield();
        }
    }
}
