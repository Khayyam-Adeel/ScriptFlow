using System.Net;
using System.Text.Json;
using FluentValidation;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Api.Middleware;

/// <summary>
/// Single place that turns exceptions into the HTTP status codes the spec calls for:
/// 400 validation, 401 unauthorized, 404 missing entity, 409 invalid prescription state.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            EntityNotFoundException => (HttpStatusCode.NotFound, "Not found"),
            InvalidPrescriptionStateException => (HttpStatusCode.Conflict, "Invalid state"),
            DuplicateNhiException => (HttpStatusCode.Conflict, "Duplicate NHI"),
            DomainException => (HttpStatusCode.BadRequest, "Invalid request"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        // Never forward the raw exception message for an unhandled/unexpected failure - it can
        // contain internal details (SQL errors, file paths). Logged above for diagnosis instead.
        var detail = statusCode == HttpStatusCode.InternalServerError
            ? title
            : exception is ValidationException validationException
                ? string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage))
                : exception.Message;

        var problem = new
        {
            title,
            status = (int)statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
