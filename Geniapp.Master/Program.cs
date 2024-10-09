using System.Text.Json;
using System.Text.Json.Serialization;
using Geniapp.Infrastructure.Database;
using Geniapp.Infrastructure.MessageQueue;
using Geniapp.Master;
using Geniapp.Master.Orchestration.Services;
using Geniapp.Master.Work;
using Microsoft.Extensions.Options;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    Guid serviceId = Guid.NewGuid();
    Log.Logger.Information("Master service {ServiceId} starting...", serviceId);

    WebApplicationBuilder builder = WebApplication.CreateBuilder();

    builder.Services.AddSerilog();

    builder.Services.AddControllers()
        .AddJsonOptions(
            opt =>
            {
                opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }
        );
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddOpenApiDocument(
        opt =>
        {
            opt.Title = "Geniapp - Master";
            opt.DocumentName = "master";
        }
    );

    builder.Services.AddSingleton<FrontendsService>();
    builder.Services.AddSingleton<WorkersService>();

    builder.Services.Configure<InitialTenantsConfiguration>(builder.Configuration.GetSection("Tenants"));
    builder.Services.Configure<PublishWorkConfiguration>(builder.Configuration.GetSection("Work"));

    builder.Services.AddHostedService<PublishWorkHostedService>();

    DatabaseConnectionStrings databaseConnections = DatabaseConnectionStrings.FromConfiguration(builder.Configuration);
    builder.Services.ConfigureSharding(databaseConnections);

    builder.Services.Configure<MessageQueueConfiguration>(builder.Configuration.GetSection("MessageQueue"));
    builder.Services.AddMessageQueue();

    WebApplication app = builder.Build();

    app.UseOpenApi();
    app.UseSwaggerUi();

    app.MapGet(
        "/",
        async request =>
        {
            request.Response.StatusCode = 200;
            await request.Response.WriteAsync($"Master service: {serviceId}");
        }
    );
    app.MapDefaultControllerRoute();

    await app.Services.RecreateShardsAsync();
    await InitializeTenantsAsync(app.Services);

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

return;

static async Task InitializeTenantsAsync(IServiceProvider services)
{
    using IServiceScope scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
    ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TenantsBootstrap");

    InitialTenantsConfiguration? configuration = scope.ServiceProvider.GetService<IOptions<InitialTenantsConfiguration>>()?.Value;
    if (configuration == null)
    {
        logger.LogInformation("Could not find {ConfigType}, no initialization will be performed.", nameof(InitialTenantsConfiguration));
        return;
    }

    CreateTenantsService createTenantsService = scope.ServiceProvider.GetRequiredService<CreateTenantsService>();

    for (int i = 0; i < configuration.Count; i++)
    {
        await createTenantsService.CreateTenantAsync();
    }

    logger.LogInformation("Created {Count} tenants.", configuration.Count);
}
