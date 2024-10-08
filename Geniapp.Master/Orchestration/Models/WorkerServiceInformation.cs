namespace Geniapp.Master.Orchestration.Models;

public class WorkerServiceInformation
{
    public required Guid Id { get; init; }
    public DateTime LastPingDate { get; set; }
}
