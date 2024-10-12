using Geniapp.Infrastructure.MessageQueue.HealthCheck;
using Geniapp.Infrastructure.MessageQueue.Work;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Geniapp.Infrastructure.MessageQueue;

public static class MessageQueueHostingExtensions
{
    public static void AddMessageQueue(this IServiceCollection services)
    {
        services.AddSingleton<MessageQueueService>();
        services.AddTransient<MessageQueueWorkAdapter>();
        services.AddTransient<MessageQueueServiceInformationAdapater>();
        services.AddSingleton<MonitorServices>();

        services.AddHostedService<PublishCurrentServiceInformation>();
        services.AddHostedService<MonitorServices>(s => s.GetRequiredService<MonitorServices>());
    }

    public static void UseMessageQueue(this IHost host)
    {
        MessageQueueService messageQueueService = host.Services.GetRequiredService<MessageQueueService>();
        messageQueueService.DeclareWorkQueue(MessageQueueWorkAdapter.WorkQueueName);
        messageQueueService.DeclareBroadcast(MessageQueueServiceInformationAdapater.ServiceInformationExchangeName);
    }
}
