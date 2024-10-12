using Geniapp.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geniapp.Master.BackgroundServices;

public class InitializeTenantsHostedService(IServiceScopeFactory scopeFactory, IOptions<InitialTenantsConfiguration>? configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TenantsBootstrap");

        InitialTenantsConfiguration? configValue = configuration?.Value;
        if (configValue == null)
        {
            logger.LogInformation("Could not find {ConfigType}, no initialization will be performed.", nameof(InitialTenantsConfiguration));
            return;
        }

        TenantsService tenantsService = scope.ServiceProvider.GetRequiredService<TenantsService>();

        int tenantsCount = await tenantsService.GetTenantsCountAsync(stoppingToken);
        if (tenantsCount >= configValue.Count)
        {
            logger.LogInformation("Found {ExistingCount} tenants, requested amount is {RequestedCount}. No tenant will be created.", tenantsCount, configValue.Count);
            return;
        }

        int toCreate = configValue.Count - tenantsCount;
        for (int i = 0; i < toCreate; i++)
        {
            await tenantsService.CreateTenantAsync(stoppingToken);
        }

        logger.LogInformation("Found {ExistingCount} tenants, created {CreatedCount} new ones. Total: {TotalCount}.", tenantsCount, toCreate, configValue.Count);
    }
}
