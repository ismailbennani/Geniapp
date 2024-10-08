using Microsoft.Extensions.DependencyInjection;

namespace Geniapp.Infrastructure.MessageQueue;

public static class MessageQueueHostingExtensions
{
    public static void ConfigureMessageQueue(this IServiceCollection services, MessageQueueConfiguration configuration)
    {
        services.AddSingleton(configuration);

        services.AddSingleton<MessageQueueService>();
        services.AddTransient<MessageQueueAdapter>();
    }
}
