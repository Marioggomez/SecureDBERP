using SecureERP.Api.Common;
using SecureERP.Application.Abstractions.Context;
using SecureERP.Domain.Exceptions;

namespace SecureERP.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContextAccessor requestContextAccessor)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            LogException(ex, context, ex.Code, LogLevel.Warning, requestContextAccessor);
            await ApiErrorWriter.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                ex.Code,
                "Request could not be processed.");
        }
        catch (Exception ex)
        {
            LogException(ex, context, "UNHANDLED_ERROR", LogLevel.Error, requestContextAccessor);
            await ApiErrorWriter.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "UNHANDLED_ERROR",
                "An unexpected error occurred.");
        }
    }

    private void LogException(
        Exception ex,
        HttpContext context,
        string errorCode,
        LogLevel level,
        IRequestContextAccessor requestContextAccessor)
    {
        var requestContext = requestContextAccessor.Current;
        using IDisposable? scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlationId"] = context.TraceIdentifier,
            ["userId"] = requestContext.UserId,
            ["tenantId"] = requestContext.TenantId,
            ["endpoint"] = context.Request.Path.Value,
            ["errorCode"] = errorCode
        });

        if (level == LogLevel.Warning)
        {
            _logger.LogWarning(ex, "Handled domain exception");
            return;
        }

        _logger.LogError(ex, "Unhandled exception");
    }
}
