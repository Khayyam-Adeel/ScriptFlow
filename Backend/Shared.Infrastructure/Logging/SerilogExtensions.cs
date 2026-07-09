using Microsoft.Extensions.Configuration;
using Serilog;

namespace Shared.Infrastructure.Logging;

public static class SerilogExtensions
{
    private const string OutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} ({CorrelationId}) {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static LoggerConfiguration CreateBaseConfiguration(IConfiguration configuration, string serviceName)
        => new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(outputTemplate: OutputTemplate);
}
