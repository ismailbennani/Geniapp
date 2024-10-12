using DockerNamesGenerator;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Logging;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Infrastructure.MessageQueue.HealthCheck;
using Geniapp.Worker;
using Geniapp.Worker.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

Log.Logger = new LoggerConfiguration().ConfigureLogging().CreateLogger();
ILogger logger = new SerilogLoggerProvider(Log.Logger).CreateLogger("Bootstrap");

try
{
    Guid serviceId = Guid.NewGuid();
    ServiceInformation currentServiceInformation = new() { ServiceId = serviceId, Name = DockerNameGeneratorFactory.Create(serviceId).GenerateName(), Type = ServiceType.Worker };
    logger.LogInformation("Worker service {Name} ({ServiceId}) starting...", currentServiceInformation.Name, currentServiceInformation.ServiceId);

    HostApplicationBuilder builder = Host.CreateApplicationBuilder();

    builder.Services.AddSerilog(cfg => cfg.ConfigureLogging());
    builder.Services.AddOptions();

    builder.Services.Configure<WorkerConfiguration>(builder.Configuration.GetSection("Work"));
    builder.Services.AddSingleton(currentServiceInformation);

    builder.Services.AddHostedService<DoWorkHostedService>();

    DatabaseConnectionStrings databaseConnections = DatabaseConnectionStrings.FromConfiguration(builder.Configuration);
    builder.Services.ConfigureSharding(databaseConnections, logger);

    builder.Services.Configure<MessageQueueConfiguration>(builder.Configuration.GetSection("MessageQueue"));
    builder.Services.AddMessageQueue();

    IHost app = builder.Build();

    app.UseMessageQueue();

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
