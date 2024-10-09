﻿using System.Reflection;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.Logging;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Worker;
using Geniapp.Worker.Work;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration().ConfigureLogging().CreateBootstrapLogger();

try
{
    Assembly thisAssembly = typeof(Program).Assembly;

    Guid serviceId = Guid.NewGuid();
    CurrentWorkerInformation currentWorkerInformation = new() { ServiceId = serviceId };
    Log.Logger.Information("Worker service {ServiceId} starting...", serviceId);

    HostApplicationBuilder builder = Host.CreateApplicationBuilder();

    builder.Configuration.AddJsonFile("appsettings.json").AddEnvironmentVariables().AddUserSecrets(thisAssembly);

    builder.Services.AddSerilog(cfg => cfg.ConfigureLogging());
    builder.Services.AddOptions();

    builder.Services.Configure<WorkerConfiguration>(builder.Configuration.GetSection("Work"));
    builder.Services.AddSingleton(currentWorkerInformation);

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
