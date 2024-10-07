namespace Geniapp.Master.Models;

public class WorkerServiceInformation
{
    public required Guid Id { get; init; }
    public DateTime LastPingDate { get; set; }
}
