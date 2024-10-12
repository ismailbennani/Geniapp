using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Geniapp.Infrastructure.MessageQueue.HealthCheck;

public class MessageQueueServiceInformationAdapater(MessageQueueService messageQueueService, ILogger<MessageQueueServiceInformationAdapater> logger)
{
    readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public const string ServiceInformationExchangeName = "service-information";

    public MessageQueueInformation GetInformation() => messageQueueService.GetQueueInformation(ServiceInformationExchangeName);

    public void PublishServiceInformation(ServiceInformation information)
    {
        string informationStr = JsonSerializer.Serialize(information, _jsonSerializerOptions);
        byte[] informationBytes = Encoding.UTF8.GetBytes(informationStr);
        messageQueueService.BroadcastMessage(ServiceInformationExchangeName, informationBytes);

        logger.LogInformation("Published service information {ServiceInformation}.", informationStr);
    }

    public void SubscribeToServiceInformation(Action<MessageToProcess<ServiceInformation>> callback)
    {
        messageQueueService.RegisterSubscriber(ServiceInformationExchangeName, Sub);
        return;

        void Sub(IModel channel, BasicDeliverEventArgs args)
        {
            string informationStr = Encoding.UTF8.GetString(args.Body.Span);
            ServiceInformation? information = JsonSerializer.Deserialize<ServiceInformation>(informationStr, _jsonSerializerOptions);
            if (information == null)
            {
                logger.LogWarning("Could not deserialize service information {ServiceInformation}.", informationStr);
                return;
            }

            MessageToProcess<ServiceInformation> message = new(channel, args, information);
            callback(message);
        }
    }
}
