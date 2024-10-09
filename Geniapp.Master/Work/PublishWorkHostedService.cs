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
) : BackgroundService
{
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
                PublishWork();
            }
            catch (Exception exn)
            {
                logger.LogError(exn, "An unexpected exception occurred while publishing new work.");
            }
        }
    }

    void PublishWork()
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
}
