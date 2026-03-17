using SecureERP.Api.Modules.Security;
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
        "/api/v1/auth/login",
        "/api/v1/auth/select-company",
        "/api/v1/auth/validate-session"
    ];

    private readonly RequestDelegate _next;

    public SecurityContextMiddleware(RequestDelegate next)
    {
        _next = next;
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
            await _next(context);
            return;
        }

        string? token = ExtractBearerToken(context.Request.Headers.Authorization);
        if (string.IsNullOrWhiteSpace(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { code = "SESSION_TOKEN_MISSING" });
            return;
        }

        ValidateSessionResult session = await validateSessionHandler.HandleAsync(
            new ValidateSessionRequest(token),
            context.RequestAborted);

        if (!session.IsValid ||
            session.UserId is null ||
            session.TenantId is null ||
            session.CompanyId is null ||
            session.SessionId is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { code = session.ErrorCode ?? "SESSION_INVALID" });
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
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "AUTHZ_DENIED",
                    reason = decision.ReasonCode
                });
                return;
            }
        }

        await _next(context);
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
}
