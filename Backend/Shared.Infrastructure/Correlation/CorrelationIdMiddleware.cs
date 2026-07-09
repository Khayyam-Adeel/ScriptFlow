using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Shared.Infrastructure.Correlation;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue) &&
                             !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        CorrelationIdAccessor.Set(correlationId);
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
