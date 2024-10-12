using Geniapp.Infrastructure.MessageQueue.HealthCheck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Geniapp.Infrastructure.Telemetry;

public static class TelemetryHostingExtensions
{
    public static void AddTelemetry(
        this IServiceCollection services,
        TelemetryConfiguration telemetryConfiguration,
        ServiceInformation currentServiceInformation,
        Action<MeterProviderBuilder>? configureMeter = null,
        ILogger? logger = null
    )
    {
        if (string.IsNullOrWhiteSpace(telemetryConfiguration.GrpcUrl))
        {
            logger?.LogWarning("Telemetry not configured properly: {UrlProperty} cannot be null or whitespace. Telemetry won't be used.", nameof(telemetryConfiguration.GrpcUrl));
            return;
        }

        OpenTelemetryBuilder builder = services.AddOpenTelemetry();

        builder.ConfigureResource(
            r => r.AddService(currentServiceInformation.Type.ToString(), currentServiceInformation.Name, serviceInstanceId: currentServiceInformation.ServiceId.ToString())
        );

        builder.WithMetrics(
            b =>
            {
                b.AddRuntimeInstrumentation()
                    .AddOtlpExporter(
                        (exporterOptions, metricReaderOptions) =>
                        {
                            exporterOptions.Endpoint = new Uri(telemetryConfiguration.GrpcUrl);
                            metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
                        }
                    );
                configureMeter?.Invoke(b);
            }
        );
    }
}
