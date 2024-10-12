﻿using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Database.ShardDatabase;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Infrastructure.MessageQueue.HealthCheck;
using Geniapp.Infrastructure.MessageQueue.Work;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geniapp.Worker.Work;

public class DoWorkHostedService(
    IServiceScopeFactory scopeFactory,
    ServiceInformation currentServiceInformation,
    IOptions<WorkerConfiguration> workerConfiguration,
    ILogger<DoWorkHostedService> logger
) : BackgroundService
{
    readonly Queue<MessageToProcess<WorkItem>> _workQueue = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // subscribe to queue
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                using IServiceScope scope = scopeFactory.CreateScope();

                MessageQueueWorkAdapter adapter = scope.ServiceProvider.GetRequiredService<MessageQueueWorkAdapter>();
                adapter.SubscribeToWork(QueueWork);
                break;
            }
            catch (Exception exn)
            {
                logger.LogError(exn, "Error while subscribing to queue.");
            }
        }

        // work periodically
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_workQueue.Count == 0)
            {
                logger.LogInformation("No work available, will idle for 1 second.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            MessageToProcess<WorkItem> work = _workQueue.Dequeue();
            await DoWorkAsync(work, stoppingToken);
        }
    }

    void QueueWork(MessageToProcess<WorkItem> obj)
    {
        _workQueue.Enqueue(obj);
        logger.LogInformation("Queued work for tenant {TenantId}.", obj.Body.TenantId);
    }

    async Task DoWorkAsync(MessageToProcess<WorkItem> obj, CancellationToken cancellationToken = default)
    {
        try
        {
            IServiceScope scope = scopeFactory.CreateScope();
            ShardContextProvider shardContextProvider = scope.ServiceProvider.GetRequiredService<ShardContextProvider>();

            logger.LogInformation("Executing work on tenant {TenantId}...", obj.Body.TenantId);
            WorkerConfiguration configuration = workerConfiguration.Value;

            double delay = Random.Shared.NextDouble() * (configuration.MaxWorkDurationInSeconds - configuration.MinWorkDurationInSeconds) + configuration.MinWorkDurationInSeconds;
            logger.LogDebug("Work on tenant {TenantId} will take {Delay} seconds.", obj.Body.TenantId, delay);

            await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

            ShardDbContext? context = await shardContextProvider.GetShardContextOfTenant(obj.Body.TenantId);
            if (context != null)
            {
                TenantData? tenant = await context.TenantsData.Include(d => d.Tenant).SingleOrDefaultAsync(d => d.Tenant.Id == obj.Body.TenantId, cancellationToken);
                if (tenant != null)
                {
                    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                    tenant.PerformWork(currentServiceInformation.ServiceId);

                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    logger.LogError("Could not find tenant {TenantId} in shard.", obj.Body.TenantId);
                }
            }
            else
            {
                logger.LogError("Could not find shard of tenant {TenantId}.", obj.Body.TenantId);
            }

            logger.LogInformation("Work on tenant {TenantId} done.", obj.Body.TenantId);

            obj.Ack();
        }
        catch (Exception exn)
        {
            logger.LogError(exn, "Error while processing work for tenant {TenantId}. Issuing Nack...", obj.Body.TenantId);

            try
            {
                obj.Nack();
            }
            catch (Exception innerExn)
            {
                logger.LogError(innerExn, "Error while issuing Nack for work for tenant {TenantId}. Work is lost.", obj.Body.TenantId);
            }
        }
    }
}
