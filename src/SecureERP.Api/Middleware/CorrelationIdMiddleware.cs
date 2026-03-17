using SecureERP.Api.Common;
using SecureERP.Application.Abstractions.Context;

namespace SecureERP.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
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
    }
}
