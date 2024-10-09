using Serilog;
using Serilog.Events;

namespace Geniapp.Infrastructure.Logging;

public static class SerilogExtensions
{
    #if DEBUG
    const LogEventLevel DefaultLoggingLevel = LogEventLevel.Debug;
    #else
    const LogEventLevel DefaultLoggingLevel = LogEventLevel.Information;
    #endif

    const LogEventLevel InfrastructureLoggingLevel = LogEventLevel.Warning;

    const string OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} ({SourceContext}){NewLine}{Exception}";

    public static LoggerConfiguration ConfigureLogging(this LoggerConfiguration configuration, LogEventLevel defaultLoggingLevel = DefaultLoggingLevel) =>
        configuration.WriteTo.Console(outputTemplate: OutputTemplate)
            .Enrich.WithProperty("SourceContext", "Bootstrap")
            .MinimumLevel.Is(defaultLoggingLevel)
            .MinimumLevel.Override("System.Net.Http.HttpClient", InfrastructureLoggingLevel)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", InfrastructureLoggingLevel)
            .MinimumLevel.Override("Microsoft.Extensions.Http", InfrastructureLoggingLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", InfrastructureLoggingLevel)
            .MinimumLevel.Override("Microsoft.Identity", InfrastructureLoggingLevel)
            .MinimumLevel.Override("Microsoft.IdentityModel", InfrastructureLoggingLevel);
}
