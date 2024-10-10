using DockerNamesGenerator;
using Geniapp.Infrastructure;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Logging;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Worker;
using Geniapp.Worker.Work;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration().ConfigureLogging().CreateLogger();

try
{
    Guid serviceId = Guid.NewGuid();
    CurrentServiceInformation currentServiceInformation = new() { ServiceId = serviceId, Name = DockerNameGeneratorFactory.Create(serviceId).GenerateName() };
    Log.Logger.Information("Master service {Name} ({ServiceId}) starting...", currentServiceInformation.Name, currentServiceInformation.ServiceId);

    HostApplicationBuilder builder = Host.CreateApplicationBuilder();

    Log.Logger.Information("Environment: {Environment}.", builder.Environment.EnvironmentName);

    builder.Services.AddSerilog(cfg => cfg.ConfigureLogging());
    builder.Services.AddOptions();

    builder.Services.Configure<WorkerConfiguration>(builder.Configuration.GetSection("Work"));
    builder.Services.AddSingleton(currentServiceInformation);

    builder.Services.AddHostedService<WorkHostedService>();

    DatabaseConnectionStrings databaseConnections = DatabaseConnectionStrings.FromConfiguration(builder.Configuration);
    builder.Services.ConfigureSharding(databaseConnections);

    builder.Services.Configure<MessageQueueConfiguration>(builder.Configuration.GetSection("MessageQueue"));
    builder.Services.AddMessageQueue();

    IHost app = builder.Build();

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
