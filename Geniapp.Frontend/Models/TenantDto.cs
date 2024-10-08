namespace Geniapp.Frontend.Models;

public class TenantDto
{
    public required Guid TenantId { get; init; }
    public required Guid ShardId { get; init; }
    public required Guid? LastWorkerId { get; init; }
    public required DateTime LastModificationDate { get; init; }
}
