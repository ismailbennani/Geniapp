namespace Geniapp.Master.Models;

public class FrontendServiceInformation
{
    public required Guid Id { get; init; }
    public DateTime LastPingDate { get; set; }
}
