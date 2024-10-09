using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Infrastructure.Work;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Geniapp.Master.Work;

public class PublishWorkHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<PublishWorkConfiguration> configuration,
    ILogger<PublishWorkHostedService> logger
) : IHostedService
{
    Timer? _timer;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StopTimerAsync();
        StartTimer();
    }

    public async Task StopAsync(CancellationToken cancellationToken) => await StopTimerAsync();

    void PublishWork(object? _)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        MasterDbContext masterDbContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        MessageQueueAdapter adapter = scope.ServiceProvider.GetRequiredService<MessageQueueAdapter>();

        int tenantsCount = masterDbContext.TenantShardAssociations.AsNoTracking().Count();
        int randomTenant = Random.Shared.Next(tenantsCount);
        TenantShardAssociation? tenantShardAssociation = masterDbContext.TenantShardAssociations.AsNoTracking().OrderBy(t => t.Id).Skip(randomTenant).FirstOrDefault();

        if (tenantShardAssociation == null)
        {
            logger.LogInformation("No tenant found, no work to publish.");
            return;
        }

        WorkItem workItem = new() { TenantId = tenantShardAssociation.TenantId };
        adapter.PublishWork(workItem);
    }

    void StartTimer()
    {
        double delay = configuration.Value.DelayInSeconds;
        _timer = new Timer(
            o =>
            {
                try
                {
                    PublishWork(o);
                }
                catch (Exception exn)
                {
                    logger.LogError(exn, "An unexpected exception occurred while publishing new work.");
                }
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(delay)
        );
        logger.LogInformation("Started publishing work every {Seconds} seconds.", delay);
    }

    async Task StopTimerAsync()
    {
        if (_timer != null)
        {
            await _timer.DisposeAsync();
            logger.LogInformation("Stopped publishing work.");
        }
    }
}
