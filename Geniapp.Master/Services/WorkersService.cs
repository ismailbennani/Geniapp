using System.Collections.Concurrent;
using Geniapp.Master.Models;

namespace Geniapp.Master.Services;

public class WorkersService
{
    readonly ConcurrentDictionary<Guid, WorkerServiceInformation> _services = [];

    public WorkerServiceInformation RegisterWorkerService(Guid workerServiceId) =>
        _services.AddOrUpdate(
            workerServiceId,
            id => new WorkerServiceInformation { Id = id, LastPingDate = DateTime.Now },
            (_, info) =>
            {
                info.LastPingDate = DateTime.Now;
                return info;
            }
        );

    public IReadOnlyCollection<WorkerServiceInformation> GetWorkerServices() => _services.Values.ToArray();
}
