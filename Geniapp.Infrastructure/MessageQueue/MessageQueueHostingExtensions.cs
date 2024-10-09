using Microsoft.Extensions.DependencyInjection;

namespace Geniapp.Infrastructure.MessageQueue;

public static class MessageQueueHostingExtensions
{
    public static void AddMessageQueue(this IServiceCollection services)
    {
        services.AddSingleton<MessageQueueService>();
        services.AddTransient<MessageQueueAdapter>();
    }
}
