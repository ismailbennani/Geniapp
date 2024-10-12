using Microsoft.Extensions.Logging;

namespace Geniapp.Infrastructure.Telemetry;

public class PrometheusClientFactory(ILoggerFactory factory)
{
    public PrometheusClient CreateClient(Uri prometheusServerUri) => new(prometheusServerUri, factory.CreateLogger<PrometheusClient>());
}
