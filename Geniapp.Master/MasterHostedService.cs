using System.Text.Json;
using System.Text.Json.Serialization;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Master.Orchestration.Services;
using Geniapp.Master.Work;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Geniapp.Master;

public class MasterHostedService : IHostedService
{
    readonly MasterConfiguration _configuration;
    WebApplication? _app;

    public MasterHostedService(MasterConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            Guid serviceId = Guid.NewGuid();

            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            builder.WebHost.UseUrls($"http://localhost:{_configuration.Port}");
            builder.Services.AddSerilog();

            builder.Services.AddControllers()
                .AddJsonOptions(
                    opt =>
                    {
                        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    }
                )
                .ConfigureApplicationPartManager(
                    m =>
                    {
                        m.ApplicationParts.Clear();
                        m.ApplicationParts.Add(new AssemblyPart(typeof(MasterHostedService).Assembly));
                    }
                );
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddOpenApiDocument(
                opt =>
                {
                    opt.Title = "Geniapp - Master";
                    opt.DocumentName = "master";
                }
            );

            builder.Services.AddSingleton<FrontendsService>();
            builder.Services.AddSingleton<WorkersService>();

            builder.Services.AddSingleton(_configuration.Work);

            builder.Services.AddHostedService<PublishWorkHostedService>();

            builder.Services.ConfigureSharding(_configuration.MasterConnectionString, _configuration.Shards);
            builder.Services.ConfigureMessageQueue(_configuration.MessageQueue);

            _app = builder.Build();

            _app.UseOpenApi();
            _app.UseSwaggerUi();

            _app.MapGet(
                "/",
                async request =>
                {
                    request.Response.StatusCode = 200;
                    await request.Response.WriteAsync($"Master service: {serviceId}", cancellationToken);
                }
            );
            _app.MapDefaultControllerRoute();

            await _app.Services.RecreateShardsAsync();

            await InitializeTenantsAsync(_app.Services, _configuration.Tenants);

            await _app.StartAsync(cancellationToken);

            Log.Logger.Information("Master service {ServiceId} started.", serviceId);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to start master service.");
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

    static async Task InitializeTenantsAsync(IServiceProvider services, InitialTenantsConfiguration configuration)
    {
        IServiceScope scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TenantsBootstrap");
        CreateTenantsService createTenantsService = scope.ServiceProvider.GetRequiredService<CreateTenantsService>();

        for (int i = 0; i < configuration.Count; i++)
        {
            await createTenantsService.CreateTenantAsync();
        }

        logger.LogInformation("Created {Count} tenants.", configuration.Count);
    }
}
