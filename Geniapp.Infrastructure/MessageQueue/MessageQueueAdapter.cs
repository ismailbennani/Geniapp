using System.Text;
using System.Text.Json;
using Geniapp.Infrastructure.Work;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace Geniapp.Infrastructure.MessageQueue;

public class MessageQueueAdapter(MessageQueueService messageQueueService, ILogger<MessageQueueAdapter> logger)
{
    readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    const string WorkQueueName = "work";

    public void PublishWork(WorkItem work)
    {
        string workStr = JsonSerializer.Serialize(work, _jsonSerializerOptions);
        byte[] workBytes = Encoding.UTF8.GetBytes(workStr);
        messageQueueService.PublishMessage(WorkQueueName, workBytes);

        logger.LogInformation("Published work item {Work}.", workStr);
    }

    public void SubscribeToWork(Action<WorkItem> callback)
    {
        messageQueueService.RegisterConsumer(WorkQueueName, Sub);
        return;

        void Sub(BasicDeliverEventArgs args)
        {
            string workStr = Encoding.UTF8.GetString(args.Body.Span);
            WorkItem? work = JsonSerializer.Deserialize<WorkItem>(workStr, _jsonSerializerOptions);
            if (work == null)
            {
                logger.LogWarning("Could not deserialize work {Work}.", workStr);
                return;
            }

            callback(work);
        }
    }
}
