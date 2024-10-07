using System.Text.Json;
using System.Text.Json.Serialization;
using Geniapp.Master.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Geniapp.Master;

public class MasterWorker : IHostedService
{
    readonly MasterConfiguration _configuration;
    WebApplication? _app;

    public MasterWorker(MasterConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

        try
        {
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
                        m.ApplicationParts.Add(new AssemblyPart(typeof(MasterWorker).Assembly));
                    }
                );
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddOpenApiDocument(
                opt =>
                {
                    opt.Title = "POC Sandbox - Master";
                    opt.DocumentName = "master";
                }
            );

            builder.Services.AddSingleton<FrontendsService>();
            builder.Services.AddSingleton<WorkersService>();

            _app = builder.Build();

            _app.UseOpenApi();
            _app.UseSwaggerUi();

            _app.MapDefaultControllerRoute();

            await _app.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
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
