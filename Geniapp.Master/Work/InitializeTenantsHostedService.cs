using Geniapp.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geniapp.Master.Work;

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

        CreateTenantsService createTenantsService = scope.ServiceProvider.GetRequiredService<CreateTenantsService>();

        for (int i = 0; i < configValue.Count; i++)
        {
            await createTenantsService.CreateTenantAsync(stoppingToken);
        }

        logger.LogInformation("Created {Count} tenants.", configValue.Count);
    }
}
