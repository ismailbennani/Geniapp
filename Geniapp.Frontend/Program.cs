using DockerNamesGenerator;
using Geniapp.Frontend.Components;
using Geniapp.Infrastructure;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Logging;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

Log.Logger = new LoggerConfiguration().ConfigureLogging().CreateBootstrapLogger();
ILogger logger = new SerilogLoggerProvider(Log.Logger).CreateLogger("Bootstrap");

try
{
    Guid serviceId = Guid.NewGuid();
    CurrentServiceInformation currentServiceInformation = new() { ServiceId = serviceId, Name = DockerNameGeneratorFactory.Create(serviceId).GenerateName() };
    logger.LogInformation("Master service {Name} ({ServiceId}) starting...", currentServiceInformation.Name, currentServiceInformation.ServiceId);

    WebApplicationBuilder builder = WebApplication.CreateBuilder();

    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

    builder.Services.AddSerilog(cfg => cfg.ConfigureLogging());
    builder.Services.AddOptions();

    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    builder.Services.AddSingleton(currentServiceInformation);

    DatabaseConnectionStrings databaseConnections = DatabaseConnectionStrings.FromConfiguration(builder.Configuration);
    builder.Services.ConfigureSharding(databaseConnections, logger);

    WebApplication app = builder.Build();

    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unexpected error occured.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
