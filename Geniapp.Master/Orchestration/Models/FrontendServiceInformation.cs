namespace Geniapp.Master.Orchestration.Models;

public class FrontendServiceInformation
{
    public required Guid Id { get; init; }
    public DateTime LastPingDate { get; set; }
}
