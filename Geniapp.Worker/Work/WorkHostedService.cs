using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Database.ShardDatabase;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Infrastructure.Work;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geniapp.Worker.Work;

public class WorkHostedService(
    MessageQueueAdapter adapter,
    WorkerConfiguration workerConfiguration,
    CurrentWorkerInformation workerInformation,
    ShardContextProvider shardContextProvider,
    ILogger<WorkerHostedService> logger
) : BackgroundService
{
    readonly Queue<MessageToProcess<WorkItem>> _workQueue = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

        adapter.SubscribeToWork(QueueWork);

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
            logger.LogInformation("Executing work on tenant {TenantId}...", obj.Body.TenantId);

            double delay = Random.Shared.NextDouble() * (workerConfiguration.MaxWorkDurationInSeconds - workerConfiguration.MinWorkDurationInSeconds)
                           + workerConfiguration.MinWorkDurationInSeconds;
            logger.LogDebug("Work on tenant {TenantId} will take {Delay} seconds.", obj.Body.TenantId, delay);

            await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

            ShardDbContext? context = await shardContextProvider.GetShardContextOfTenant(obj.Body.TenantId);
            if (context != null)
            {
                TenantData? tenant = await context.TenantsData.AsNoTracking().SingleOrDefaultAsync(d => d.Tenant.Id == obj.Body.TenantId, cancellationToken);
                if (tenant != null)
                {
                    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                    tenant.PerformWork(workerInformation.ServiceId);

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
            logger.LogError(exn, "Error while processing work for tenant {TenantId}. Work will be requeued.", obj.Body.TenantId);
            obj.Nack();
        }
    }
}
