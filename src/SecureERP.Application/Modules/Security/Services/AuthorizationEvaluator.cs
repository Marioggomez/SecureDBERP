using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Services;

public sealed class AuthorizationEvaluator : IAuthorizationEvaluator
{
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public AuthorizationEvaluator(
        IAuthorizationRepository authorizationRepository,
        IRequestContextAccessor requestContextAccessor)
    {
        _authorizationRepository = authorizationRepository;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<AuthorizationCheckResult> EvaluateAsync(
        AuthorizationCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null || context.SessionId is null)
        {
            return new AuthorizationCheckResult(false, "SESSION_CONTEXT_REQUIRED", "CONTEXT");
        }

        AuthorizationDecision decision = await _authorizationRepository.EvaluateAsync(
            new AuthorizationCheck(
                context.UserId.Value,
                context.TenantId.Value,
                context.CompanyId.Value,
                context.SessionId.Value,
                request.PermissionCode,
                request.RequiresMfa),
            cancellationToken);

        return new AuthorizationCheckResult(decision.IsAllowed, decision.ReasonCode, decision.ResolutionSource);
    }
}
