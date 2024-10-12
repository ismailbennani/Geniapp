using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geniapp.Infrastructure.MessageQueue.HealthCheck;

public class MonitorServices(MessageQueueServiceInformationAdapater adapater, ILogger<MonitorServices> logger) : BackgroundService
{
    static readonly TimeSpan LogDelay = TimeSpan.FromSeconds(5);
    static readonly TimeSpan InactiveDelay = TimeSpan.FromSeconds(10);
    readonly ConcurrentDictionary<Guid, ServiceState> _states = [];

    public IEnumerable<ServiceInformation> ActiveServices {
        get {
            DateTime inactiveDate = DateTime.Now - InactiveDelay;
            return _states.Values.Where(s => s.LastReceptionDate >= inactiveDate).Select(s => s.LastInformation);
        }
    }

    public IEnumerable<ServiceInformation> InactiveServices {
        get {
            DateTime inactiveDate = DateTime.Now - InactiveDelay;
            return _states.Values.Where(s => s.LastReceptionDate < inactiveDate).Select(s => s.LastInformation);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        adapater.SubscribeToServiceInformation(UpdateState);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(LogDelay, stoppingToken);

            IEnumerable<ServiceInformation> activeServices = ActiveServices.ToArray();
            IEnumerable<ServiceInformation> inactiveServices = InactiveServices.ToArray();

            logger.LogInformation(
                "Active services: {MasterCount} master(s), {WorkerCount} worker(s), {FrontendCount} frontend(s).",
                activeServices.Count(i => i.Type == ServiceType.Master),
                activeServices.Count(i => i.Type == ServiceType.Worker),
                activeServices.Count(i => i.Type == ServiceType.Frontend)
            );

            logger.LogInformation(
                "Inactive services: {MasterCount} master(s), {WorkerCount} worker(s), {FrontendCount} frontend(s).",
                inactiveServices.Count(i => i.Type == ServiceType.Master),
                inactiveServices.Count(i => i.Type == ServiceType.Worker),
                inactiveServices.Count(i => i.Type == ServiceType.Frontend)
            );
        }
    }

    void UpdateState(MessageToProcess<ServiceInformation> message)
    {
        _states.AddOrUpdate(message.Body.ServiceId, _ => new ServiceState(message.Body), (_, state) => state.Update(message.Body));
        message.Ack();
    }

    class ServiceState
    {
        public ServiceState(ServiceInformation lastInformation)
        {
            LastInformation = lastInformation;
            LastReceptionDate = DateTime.Now;
        }

        public ServiceInformation LastInformation { get; private set; }
        public DateTime LastReceptionDate { get; private set; }

        public ServiceState Update(ServiceInformation information)
        {
            LastInformation = information;
            LastReceptionDate = DateTime.Now;
            return this;
        }
    }
}
