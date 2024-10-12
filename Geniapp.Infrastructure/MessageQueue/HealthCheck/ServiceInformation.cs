namespace Geniapp.Infrastructure.MessageQueue.HealthCheck;

public class ServiceInformation
{
    public required Guid ServiceId { get; init; }
    public required string Name { get; init; }
    public required ServiceType Type { get; init; }
}
