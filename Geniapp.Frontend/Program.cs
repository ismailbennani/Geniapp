using System.Text.Json;
using System.Text.Json.Serialization;
using Geniapp.Infrastructure.Database;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    Guid serviceId = Guid.NewGuid();
    Log.Logger.Information("Frontend service {ServiceId} starting...", serviceId);

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
            opt.Title = "Geniapp - Frontend";
            opt.DocumentName = "frontend";
        }
    );

    DatabaseConnectionStrings databaseConnections = DatabaseConnectionStrings.FromConfiguration(builder.Configuration);
    builder.Services.ConfigureSharding(databaseConnections);

    WebApplication app = builder.Build();

    app.UseOpenApi();
    app.UseSwaggerUi();

    app.MapGet(
        "/",
        async request =>
        {
            request.Response.StatusCode = 200;
            await request.Response.WriteAsync($"Frontend service: {serviceId}");
        }
    );
    app.MapDefaultControllerRoute();

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
