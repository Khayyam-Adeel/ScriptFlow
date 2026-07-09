using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Correlation;

namespace ScriptFlow.API.Application.Behaviors;

/// <summary>
/// Logs entry, exit, duration, and correlation ID for every command/query, so the full
/// application call trace can be followed and errors located from the logs alone.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICorrelationIdAccessor correlationIdAccessor)
    {
        _logger = logger;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = _correlationIdAccessor.CorrelationId;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling {RequestName} [{CorrelationId}]", requestName, correlationId);

        try
        {
            var response = await next();
            _logger.LogInformation(
                "Handled {RequestName} [{CorrelationId}] in {ElapsedMilliseconds}ms",
                requestName, correlationId, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed handling {RequestName} [{CorrelationId}] after {ElapsedMilliseconds}ms",
                requestName, correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
