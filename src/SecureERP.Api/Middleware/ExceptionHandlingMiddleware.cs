using SecureERP.Api.Common;
using SecureERP.Domain.Exceptions;

namespace SecureERP.Api.Middleware;

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
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error: {Code}", ex.Code);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "UNHANDLED_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string code, string detail)
    {
        string correlationId = context.TraceIdentifier;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title = "Request failed",
            status,
            code,
            detail,
            correlationId,
            traceIdHeader = ApiConstants.CorrelationIdHeader
        });
    }
}
