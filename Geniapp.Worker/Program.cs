using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Worker;
using Geniapp.Worker.Work;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    Guid serviceId = Guid.NewGuid();
    CurrentWorkerInformation currentWorkerInformation = new() { ServiceId = serviceId };
    Log.Logger.Information("Worker service {ServiceId} starting...", serviceId);

    HostApplicationBuilder builder = Host.CreateApplicationBuilder();

    builder.Services.AddSerilog();
    builder.Services.AddOptions();

    builder.Services.Configure<WorkerConfiguration>(builder.Configuration.GetSection("Work"));
    builder.Services.AddSingleton(currentWorkerInformation);

    builder.Services.AddHostedService<WorkHostedService>();

    DatabaseConnectionStrings databaseConnections = DatabaseConnectionStrings.FromConfiguration(builder.Configuration);
    builder.Services.ConfigureSharding(databaseConnections);

    builder.Services.Configure<MessageQueueConfiguration>(builder.Configuration.GetSection("MessageQueue"));
    builder.Services.AddMessageQueue();

    IHost app = builder.Build();

    await app.StartAsync();

}
catch (Exception ex)
{
    Log.Fatal(ex, "An unexpected error occured.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
