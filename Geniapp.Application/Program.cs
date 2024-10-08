using System.Text.Json;
using CommandLine;
using Geniapp.Application;
using Geniapp.Application.Configuration;
using Geniapp.Frontend;
using Geniapp.Infrastructure.Database;
using Geniapp.Master;
using Geniapp.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

if (EF.IsDesignTime)
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
    builder.Services.ConfigureSharding("MASTER_CONNECTION_STRING", [new ShardConfiguration { Name = "SHARD", ConnectionString = "SHARD_CONNECTION_STRING" }]);
    IHost app = builder.Build();
    await app.StartAsync();
}
else
{
    await Parser.Default.ParseArguments<RunArguments>(args).WithParsedAsync(StartAsync);
}

return;

async Task StartAsync(RunArguments runArgs)
{
    Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

    try
    {
        AgentConfiguration? configuration = ParseConfiguration(runArgs.ConfigurationFile);
        Log.Logger.Information("Found configuration: {Configuration}", JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true }));

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSerilog(c => c.WriteTo.Console().MinimumLevel.Is(runArgs.Verbose ? LogEventLevel.Debug : LogEventLevel.Information));

        if (configuration?.Master?.Enabled == true)
        {
            Log.Logger.Information("Master service enabled.");
            builder.Services.AddHostedService<MasterHostedService>(
                _ => new MasterHostedService(
                    new MasterConfiguration
                    {
                        MasterConnectionString = configuration.MasterConnectionString,
                        Shards = configuration.Shards,
                        MessageQueue = configuration.MessageQueue,
                        Port = configuration.Master.Port,
                        Work = configuration.Master.Work
                    }
                )
            );
        }

        if (configuration?.Worker?.Enabled == true)
        {
            Log.Logger.Information("Worker service enabled.");
            builder.Services.AddHostedService<WorkerHostedService>(
                _ => new WorkerHostedService(
                    new WorkerConfiguration
                    {
                        MasterConnectionString = configuration.MasterConnectionString,
                        Shards = configuration.Shards,
                        MessageQueue = configuration.MessageQueue,
                        MinWorkDurationInSeconds = configuration.Worker.MinWorkDurationInSeconds,
                        MaxWorkDurationInSeconds = configuration.Worker.MaxWorkDurationInSeconds
                    }
                )
            );
        }

        if (configuration?.Frontend?.Enabled == true)
        {
            Log.Logger.Information("Frontend service enabled.");
            builder.Services.AddHostedService<FrontendHostedService>(
                _ => new FrontendHostedService(
                    new FrontendConfiguration
                    {
                        MasterConnectionString = configuration.MasterConnectionString,
                        Shards = configuration.Shards,
                        MessageQueue = configuration.MessageQueue
                    }
                )
            );
        }

        IHost host = builder.Build();

        await host.RunAsync();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application terminated unexpectedly.");
    }
    finally
    {
        await Log.CloseAndFlushAsync();
    }
}

AgentConfiguration? ParseConfiguration(string path)
{
    if (!File.Exists(path))
    {
        return null;
    }

    IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
    using FileStream file = File.OpenRead(path);
    using StreamReader reader = new(file);
    return deserializer.Deserialize<AgentConfiguration>(reader);
}

namespace Geniapp.Application
{
    class RunArguments
    {
        [Value(0, MetaName = "CONFIG", Default = "config.yml", HelpText = "The path to the configuration file.")]
        public string ConfigurationFile { get; set; } = "config.yml";

        [Option('v', "verbose", Default = false, HelpText = "Print additional information.")]
        public bool Verbose { get; set; } = false;
    }
}
