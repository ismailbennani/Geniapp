using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Infrastructure.Work;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geniapp.Worker.Work;

public class WorkHostedService(MessageQueueAdapter adapter, WorkerConfiguration configuration, ILogger<WorkerHostedService> logger) : BackgroundService
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
            await DoWorkAsync(work);
        }
    }

    void QueueWork(MessageToProcess<WorkItem> obj)
    {
        _workQueue.Enqueue(obj);
        logger.LogInformation("Queued work for tenant {TenantId}.", obj.Body.TenantId);
    }

    async Task DoWorkAsync(MessageToProcess<WorkItem> obj)
    {
        try
        {
            logger.LogInformation("Executing work on tenant {TenantId}...", obj.Body.TenantId);

            double delay = Random.Shared.NextDouble() * (configuration.MaxWorkDurationInSeconds - configuration.MinWorkDurationInSeconds) + configuration.MinWorkDurationInSeconds;
            logger.LogDebug("Work on tenant {TenantId} will take {Delay} seconds.", obj.Body.TenantId, delay);

            await Task.Delay(TimeSpan.FromSeconds(delay));

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
