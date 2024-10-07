using Geniapp.Infrastructure.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Geniapp.Frontend;

public class FrontendHostedService : IHostedService
{
    readonly FrontendConfiguration _configuration;
    WebApplication? _app;

    public FrontendHostedService(FrontendConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            Guid serviceId = Guid.NewGuid();

            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            builder.Services.AddSerilog();
            builder.Services.AddServerSideBlazor(opt => opt.DetailedErrors = true);

            builder.Services.ConfigureSharding(_configuration.MasterConnectionString, _configuration.Shards);

            _app = builder.Build();

            _app.MapBlazorHub();

            await _app.StartAsync(cancellationToken);

            Log.Logger.Information("Frontend service {ServiceId} started.", serviceId);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to start frontend service.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_app != null)
        {
            await _app.StopAsync(cancellationToken);
            _app = null;
        }
    }
}
