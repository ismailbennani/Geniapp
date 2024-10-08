using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Infrastructure.Work;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geniapp.Worker.Work;

public class WorkHostedService(MessageQueueAdapter adapter, ILogger<WorkerHostedService> logger) : IHostedService
{
    readonly Queue<WorkItem> _work = [];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        adapter.SubscribeToWork(QueueWork);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_work.Count == 0)
            {
                logger.LogInformation("No work available, will idle for 1 second.");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                continue;
            }

            WorkItem work = _work.Dequeue();
            await DoWorkAsync(work);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    void QueueWork(WorkItem obj)
    {
        _work.Enqueue(obj);
        logger.LogInformation("Queued work for tenant {TenantId}.", obj.TenantId);
    }

    async Task DoWorkAsync(WorkItem obj)
    {
        logger.LogInformation("Executing work on tenant {TenantId}...", obj.TenantId);
        double delay = Random.Shared.NextDouble();
        await Task.Delay(TimeSpan.FromSeconds(delay));
        logger.LogInformation("Work on tenant {TenantId} done.", obj.TenantId);
    }
}
