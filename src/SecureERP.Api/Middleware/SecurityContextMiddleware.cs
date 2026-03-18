using SecureERP.Api.Modules.Security;
using SecureERP.Api.Common;
using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Api.Middleware;

public sealed class SecurityContextMiddleware
{
    private static readonly string[] PublicPaths =
    [
        "/",
        "/health",
        "/health/ready",
        "/health/live",
        "/api/v1/auth/login",
        "/api/v1/auth/mfa/challenge",
        "/api/v1/auth/mfa/verify",
        "/api/v1/auth/select-company"
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityContextMiddleware> _logger;

    public SecurityContextMiddleware(RequestDelegate next, ILogger<SecurityContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRequestContextAccessor requestContextAccessor,
        IValidateSessionHandler validateSessionHandler,
        IAuthorizationEvaluator authorizationEvaluator)
    {
        string path = context.Request.Path.Value ?? string.Empty;
        if (IsPublicPath(path))
        {
            await TrySetRequestContextFromBearerTokenAsync(context, requestContextAccessor, validateSessionHandler);
            await _next(context);
            return;
        }

        string? token = ExtractBearerToken(context.Request.Headers.Authorization);
        if (string.IsNullOrWhiteSpace(token))
        {
            await WriteUnauthorizedAsync(context, requestContextAccessor, "SESSION_INVALID", "Session is invalid or expired.");
            return;
        }

        ValidateSessionResult session = await validateSessionHandler.HandleAsync(
            new ValidateSessionRequest(token, 30, true, context.Connection.RemoteIpAddress?.ToString()),
            context.RequestAborted);

        if (!session.IsValid ||
            session.UserId is null ||
            session.TenantId is null ||
            session.CompanyId is null ||
            session.SessionId is null)
        {
            string sessionCode = session.ErrorCode is "SESSION_EXPIRED" or "SESSION_INVALID"
                ? session.ErrorCode
                : "SESSION_INVALID";
            await WriteUnauthorizedAsync(context, requestContextAccessor, sessionCode, "Session is invalid or expired.");
            return;
        }

        RequestContext current = requestContextAccessor.Current;
        requestContextAccessor.SetCurrent(new RequestContext(
            session.TenantId,
            session.CompanyId,
            session.UserId,
            session.SessionId,
            current.CorrelationId));

        RequirePermissionAttribute? permissionRequirement = context.GetEndpoint()?.Metadata.GetMetadata<RequirePermissionAttribute>();
        if (permissionRequirement is not null)
        {
            AuthorizationCheckResult decision = await authorizationEvaluator.EvaluateAsync(
                new AuthorizationCheckRequest(
                    permissionRequirement.PermissionCode,
                    permissionRequirement.RequiresMfa,
                    path,
                    context.Request.Method,
                    context.Connection.RemoteIpAddress?.ToString(),
                    context.Request.Headers.UserAgent.ToString(),
                    context.TraceIdentifier),
                context.RequestAborted);

            if (!decision.IsAllowed)
            {
                RequestContext scoped = requestContextAccessor.Current;
                using IDisposable? scope = _logger.BeginScope(new Dictionary<string, object?>
                {
                    ["correlationId"] = context.TraceIdentifier,
                    ["userId"] = scoped.UserId,
                    ["tenantId"] = scoped.TenantId,
                    ["endpoint"] = path,
                    ["errorCode"] = "AUTHZ_DENIED"
                });
                _logger.LogWarning("Authorization denied with reason {ReasonCode}", decision.ReasonCode);

                await ApiErrorWriter.WriteAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "AUTHZ_DENIED",
                    "Operation is not authorized.");
                return;
            }
        }

        await _next(context);
    }

    private static async Task TrySetRequestContextFromBearerTokenAsync(
        HttpContext context,
        IRequestContextAccessor requestContextAccessor,
        IValidateSessionHandler validateSessionHandler)
    {
        string? token = ExtractBearerToken(context.Request.Headers.Authorization);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        ValidateSessionResult session = await validateSessionHandler.HandleAsync(
            new ValidateSessionRequest(token, 30, true, context.Connection.RemoteIpAddress?.ToString()),
            context.RequestAborted);

        if (!session.IsValid ||
            session.UserId is null ||
            session.TenantId is null ||
            session.SessionId is null)
        {
            return;
        }

        RequestContext current = requestContextAccessor.Current;
        requestContextAccessor.SetCurrent(new RequestContext(
            session.TenantId,
            session.CompanyId,
            session.UserId,
            session.SessionId,
            current.CorrelationId));
    }

    private static bool IsPublicPath(string path)
    {
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return PublicPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        const string prefix = "Bearer ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorizationHeader[prefix.Length..].Trim();
    }

    private async Task WriteUnauthorizedAsync(
        HttpContext context,
        IRequestContextAccessor requestContextAccessor,
        string errorCode,
        string message)
    {
        RequestContext scoped = requestContextAccessor.Current;
        using IDisposable? scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlationId"] = context.TraceIdentifier,
            ["userId"] = scoped.UserId,
            ["tenantId"] = scoped.TenantId,
            ["endpoint"] = context.Request.Path.Value,
            ["errorCode"] = errorCode
        });
        _logger.LogWarning("Security context request denied");

        await ApiErrorWriter.WriteAsync(
            context,
            StatusCodes.Status401Unauthorized,
            errorCode,
            message,
            context.RequestAborted);
    }
}
