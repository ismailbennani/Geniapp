using System.Collections.Concurrent;
using Geniapp.Master.Orchestration.Models;

namespace Geniapp.Master.Orchestration.Services;

public class FrontendsService
{
    readonly ConcurrentDictionary<Guid, FrontendServiceInformation> _services = [];

    public FrontendServiceInformation RegisterFrontendService(Guid frontendServiceId) =>
        _services.AddOrUpdate(
            frontendServiceId,
            id => new FrontendServiceInformation { Id = id, LastPingDate = DateTime.Now },
            (_, info) =>
            {
                info.LastPingDate = DateTime.Now;
                return info;
            }
        );

    public IReadOnlyCollection<FrontendServiceInformation> GetFrontendServices() => _services.Values.ToArray();
}
