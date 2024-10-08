using System.Text.Json;
using System.Text.Json.Serialization;
using Geniapp.Infrastructure.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
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

            builder.WebHost.UseUrls($"http://127.0.0.1:{_configuration.Port}");
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
                        m.ApplicationParts.Add(new AssemblyPart(typeof(FrontendHostedService).Assembly));
                    }
                );
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddOpenApiDocument(
                opt =>
                {
                    opt.Title = "Geniapp - Frontend";
                    opt.DocumentName = "frontend";
                }
            );

            builder.Services.ConfigureSharding(_configuration.MasterConnectionString, _configuration.Shards);

            _app = builder.Build();

            _app.UseOpenApi();
            _app.UseSwaggerUi();

            _app.MapGet(
                "/",
                async request =>
                {
                    request.Response.StatusCode = 200;
                    await request.Response.WriteAsync($"Frontend service: {serviceId}", cancellationToken);
                }
            );
            _app.MapDefaultControllerRoute();

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
