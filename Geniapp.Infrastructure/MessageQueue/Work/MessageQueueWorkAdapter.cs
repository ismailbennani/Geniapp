using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Geniapp.Infrastructure.MessageQueue.Work;

public class MessageQueueWorkAdapter(MessageQueueService messageQueueService, ILogger<MessageQueueWorkAdapter> logger)
{
    readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public const string WorkQueueName = "work";

    public MessageQueueInformation GetInformation() => messageQueueService.GetQueueInformation(WorkQueueName);

    public void PublishWork(WorkItem work)
    {
        string workStr = JsonSerializer.Serialize(work, _jsonSerializerOptions);
        byte[] workBytes = Encoding.UTF8.GetBytes(workStr);
        messageQueueService.PublishWork(WorkQueueName, workBytes);

        logger.LogInformation("Published work item {Work}.", workStr);
    }

    public void SubscribeToWork(Action<MessageToProcess<WorkItem>> callback)
    {
        messageQueueService.RegisterWorker(WorkQueueName, Sub);
        return;

        void Sub(IModel channel, BasicDeliverEventArgs args)
        {
            string workStr = Encoding.UTF8.GetString(args.Body.Span);
            WorkItem? work = JsonSerializer.Deserialize<WorkItem>(workStr, _jsonSerializerOptions);
            if (work == null)
            {
                logger.LogWarning("Could not deserialize work {Work}.", workStr);
                return;
            }

            MessageToProcess<WorkItem> message = new(channel, args, work);
            callback(message);
        }
    }
}
