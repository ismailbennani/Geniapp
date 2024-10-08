using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Infrastructure.Work;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geniapp.Master.Work;

public class PublishWorkHostedService(
    MessageQueueAdapter adapter,
    PublishWorkConfiguration configuration,
    MasterDbContext masterDbContext,
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
        try
        {
            int tenantsCount = masterDbContext.TenantShardAssociations.AsNoTracking().Count();
            int randomTenant = Random.Shared.Next(tenantsCount);
            TenantShardAssociation? tenantShardAssociation = masterDbContext.TenantShardAssociations.AsNoTracking().Skip(randomTenant).FirstOrDefault();

            if (tenantShardAssociation == null)
            {
                logger.LogInformation("No tenant found, no work to publish.");
                return;
            }

            WorkItem workItem = new() { TenantId = tenantShardAssociation.TenantId };
            adapter.PublishWork(workItem);
        }
        catch (Exception exn)
        {
            logger.LogError(exn, "An unexpected exception occurred while publishing new work.");
        }
    }

    void StartTimer()
    {
        double delay = configuration.DelayInSeconds;
        _timer = new Timer(PublishWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(delay));
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
