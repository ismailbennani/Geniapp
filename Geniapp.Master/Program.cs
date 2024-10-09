using System.Reflection;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Logging;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Master;
using Geniapp.Master.Work;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration().ConfigureLogging().CreateBootstrapLogger();

try
{
    Assembly thisAssembly = typeof(Program).Assembly;

    Guid serviceId = Guid.NewGuid();
    Log.Logger.Information("Master service {ServiceId} starting...", serviceId);

    HostApplicationBuilder builder = Host.CreateApplicationBuilder();

    builder.Configuration.AddJsonFile("appsettings.json").AddEnvironmentVariables().AddUserSecrets(thisAssembly);

    builder.Services.AddSerilog(cfg => cfg.ConfigureLogging());
    builder.Services.AddOptions();

    builder.Services.Configure<InitialTenantsConfiguration>(builder.Configuration.GetSection("Tenants"));
    builder.Services.Configure<PublishWorkConfiguration>(builder.Configuration.GetSection("Work"));

    builder.Services.AddHostedService<PublishWorkHostedService>();
    builder.Services.AddHostedService<InitializeTenantsHostedService>();

    DatabaseConnectionStrings databaseConnections = DatabaseConnectionStrings.FromConfiguration(builder.Configuration);
    builder.Services.ConfigureSharding(databaseConnections);

    builder.Services.Configure<MessageQueueConfiguration>(builder.Configuration.GetSection("MessageQueue"));
    builder.Services.AddMessageQueue();

    IHost app = builder.Build();

    await app.Services.RecreateShardsAsync();

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
