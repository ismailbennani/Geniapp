using System.Diagnostics.Metrics;
using Geniapp.Infrastructure.Database;
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

namespace Geniapp.Worker.BackgroundServices;

public class DoWorkHostedService : BackgroundService
{
    public const string MeterName = "work";

    readonly Queue<MessageToProcess<WorkItem>> _workQueue = [];
    readonly Counter<int> _workQueuedCounter;
    readonly Counter<int> _workCompletedCounter;
    readonly Counter<int> _workFailedCounter;
    readonly IServiceScopeFactory _scopeFactory;
    readonly ServiceInformation _currentServiceInformation;
    readonly IOptions<WorkerConfiguration> _workerConfiguration;
    readonly ILogger<DoWorkHostedService> _logger;

    public DoWorkHostedService(
        IServiceScopeFactory scopeFactory,
        ServiceInformation currentServiceInformation,
        IOptions<WorkerConfiguration> workerConfiguration,
        ILogger<DoWorkHostedService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _currentServiceInformation = currentServiceInformation;
        _workerConfiguration = workerConfiguration;
        _logger = logger;

        WorkMeter = new Meter(MeterName);
        _workQueuedCounter = WorkMeter.CreateCounter<int>("work.queued.count", "{workitem}", "Counts the work items that have been queued.");
        _workCompletedCounter = WorkMeter.CreateCounter<int>("work.completed.count", "{workitem}", "Counts the work items that have been completed.");
        _workFailedCounter = WorkMeter.CreateCounter<int>("work.failed.count", "{workitem}", "Counts the work items that failed.");
    }

    public Meter WorkMeter { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // subscribe to queue
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                using IServiceScope scope = _scopeFactory.CreateScope();

                MessageQueueWorkAdapter adapter = scope.ServiceProvider.GetRequiredService<MessageQueueWorkAdapter>();
                adapter.SubscribeToWork(QueueWork);
                break;
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, "Error while subscribing to queue.");
            }
        }

        // work periodically
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_workQueue.Count == 0)
            {
                _logger.LogInformation("No work available, will idle for 1 second.");
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
        _workQueuedCounter.Add(1);
        _logger.LogInformation("Queued work for tenant {TenantId}.", obj.Body.TenantId);
    }

    async Task DoWorkAsync(MessageToProcess<WorkItem> obj, CancellationToken cancellationToken = default)
    {
        try
        {
            IServiceScope scope = _scopeFactory.CreateScope();
            ShardContextProvider shardContextProvider = scope.ServiceProvider.GetRequiredService<ShardContextProvider>();

            _logger.LogInformation("Executing work on tenant {TenantId}...", obj.Body.TenantId);
            WorkerConfiguration configuration = _workerConfiguration.Value;

            double delay = Random.Shared.NextDouble() * (configuration.MaxWorkDurationInSeconds - configuration.MinWorkDurationInSeconds) + configuration.MinWorkDurationInSeconds;
            _logger.LogDebug("Work on tenant {TenantId} will take {Delay} seconds.", obj.Body.TenantId, delay);

            await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

            ShardDbContext? context = await shardContextProvider.GetShardContextOfTenant(obj.Body.TenantId);
            if (context != null)
            {
                TenantData? tenant = await context.TenantsData.Include(d => d.Tenant).SingleOrDefaultAsync(d => d.Tenant.Id == obj.Body.TenantId, cancellationToken);
                if (tenant != null)
                {
                    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                    tenant.PerformWork(_currentServiceInformation.ServiceId);

                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    _logger.LogError("Could not find tenant {TenantId} in shard.", obj.Body.TenantId);
                }
            }
            else
            {
                _logger.LogError("Could not find shard of tenant {TenantId}.", obj.Body.TenantId);
            }

            obj.Ack();

            _workCompletedCounter.Add(1);
            _logger.LogInformation("Work on tenant {TenantId} done.", obj.Body.TenantId);
        }
        catch (Exception exn)
        {
            _logger.LogError(exn, "Error while processing work for tenant {TenantId}. Issuing Nack...", obj.Body.TenantId);

            try
            {
                _workFailedCounter.Add(1);
                obj.Nack();
            }
            catch (Exception innerExn)
            {
                _logger.LogError(innerExn, "Error while issuing Nack for work for tenant {TenantId}. Work is lost.", obj.Body.TenantId);
            }
        }
    }
}
