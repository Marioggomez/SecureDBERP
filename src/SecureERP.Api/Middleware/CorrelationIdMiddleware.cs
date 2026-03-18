using SecureERP.Api.Common;
using SecureERP.Application.Abstractions.Context;

namespace SecureERP.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContextAccessor requestContextAccessor)
    {
        string? incoming = context.Request.Headers[ApiConstants.CorrelationIdHeader].FirstOrDefault();
        string correlationId = string.IsNullOrWhiteSpace(incoming) ? Guid.NewGuid().ToString("N") : incoming.Trim();

        context.TraceIdentifier = correlationId;
        context.Response.Headers[ApiConstants.CorrelationIdHeader] = correlationId;

        RequestContext current = requestContextAccessor.Current;
        requestContextAccessor.SetCurrent(current with { CorrelationId = correlationId });

        await _next(context);

        RequestContext updated = requestContextAccessor.Current;
        string? errorCode = context.Items.TryGetValue(ApiErrorWriter.ErrorCodeItemKey, out object? value)
            ? value?.ToString()
            : null;

        using IDisposable? scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["userId"] = updated.UserId,
            ["tenantId"] = updated.TenantId,
            ["endpoint"] = context.Request.Path.Value,
            ["errorCode"] = errorCode
        });

        if (context.Response.StatusCode >= 500)
        {
            _logger.LogError("Request completed with server error status {StatusCode}", context.Response.StatusCode);
        }
        else if (context.Response.StatusCode >= 400)
        {
            _logger.LogWarning("Request completed with client error status {StatusCode}", context.Response.StatusCode);
        }
        else
        {
            _logger.LogInformation("Request completed with status {StatusCode}", context.Response.StatusCode);
        }
    }
}
