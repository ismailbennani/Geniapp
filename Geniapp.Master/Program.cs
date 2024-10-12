using DockerNamesGenerator;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Logging;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Infrastructure.MessageQueue.HealthCheck;
using Geniapp.Infrastructure.Telemetry;
using Geniapp.Master;
using Geniapp.Master.BackgroundServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

Log.Logger = new LoggerConfiguration().ConfigureLogging().CreateBootstrapLogger();
ILogger logger = new SerilogLoggerProvider(Log.Logger).CreateLogger("Bootstrap");

try
{
    Guid serviceId = Guid.NewGuid();
    ServiceInformation currentServiceInformation = new() { ServiceId = serviceId, Name = DockerNameGeneratorFactory.Create(serviceId).GenerateName(), Type = ServiceType.Master };
    logger.LogInformation("Master service {Name} ({ServiceId}) starting...", currentServiceInformation.Name, currentServiceInformation.ServiceId);

    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog(cfg => cfg.ConfigureLogging());
    builder.Services.AddOptions();

    builder.Services.Configure<InitialTenantsConfiguration>(builder.Configuration.GetSection("Tenants"));
    builder.Services.Configure<PublishWorkConfiguration>(builder.Configuration.GetSection("Work"));
    builder.Services.AddSingleton(currentServiceInformation);

    builder.Services.AddHostedService<PublishWorkHostedService>();
    builder.Services.AddHostedService<InitializeTenantsHostedService>();

    DatabaseConnectionStrings databaseConnections = DatabaseConnectionStrings.FromConfiguration(builder.Configuration);
    builder.Services.ConfigureSharding(databaseConnections, logger);

    builder.Services.Configure<MessageQueueConfiguration>(builder.Configuration.GetSection("MessageQueue"));
    builder.Services.AddMessageQueue();

    TelemetryConfiguration telemetryConfiguration = new();
    builder.Configuration.GetSection("Telemetry").Bind(telemetryConfiguration);
    builder.Services.AddTelemetry(telemetryConfiguration, currentServiceInformation, logger: logger);

    IHost app = builder.Build();

    app.UseMessageQueue();

    InitialTenantsConfiguration initialTenantsConfiguration = app.Services.GetRequiredService<IOptions<InitialTenantsConfiguration>>().Value;
    if (initialTenantsConfiguration.Recreate)
    {
        await app.Services.RecreateDatabasesAsync();
    }
    else
    {
        await app.Services.EnsureDatabasesCreatedAsync();
    }

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
