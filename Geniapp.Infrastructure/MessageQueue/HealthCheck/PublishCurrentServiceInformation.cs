using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geniapp.Infrastructure.MessageQueue.HealthCheck;

public class PublishCurrentServiceInformation(
    IServiceScopeFactory scopeFactory,
    ServiceInformation currentServiceInformation,
    ILogger<PublishCurrentServiceInformation> logger
) : BackgroundService
{
    static readonly TimeSpan Delay = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                MessageQueueServiceInformationAdapater adapter = scope.ServiceProvider.GetRequiredService<MessageQueueServiceInformationAdapater>();

                adapter.PublishServiceInformation(currentServiceInformation);
            }
            catch (Exception exn)
            {
                logger.LogError(exn, "An unexpected error occurred while publishing current service information");
                await Task.Delay(Delay * 10, stoppingToken);
            }

            await Task.Delay(Delay, stoppingToken);
        }
    }
}
