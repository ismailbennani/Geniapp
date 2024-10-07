using Geniapp.Infrastructure.Database;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Geniapp.Worker;

public class WorkerHostedService : IHostedService
{
    readonly WorkerConfiguration _configuration;
    IHost? _host;

    public WorkerHostedService(WorkerConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            Guid serviceId = Guid.NewGuid();

            HostApplicationBuilder builder = Host.CreateApplicationBuilder();

            builder.Services.AddSerilog();
            builder.Services.ConfigureSharding(_configuration.MasterConnectionString, _configuration.Shards);

            _host = builder.Build();

            await _host.StartAsync(cancellationToken);

            Log.Logger.Information("Worker service {ServiceId} started.", serviceId);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to start worker service.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_host != null)
        {
            await _host.StopAsync(cancellationToken);
            _host = null;
        }
    }
}
