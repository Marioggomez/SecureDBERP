using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface IAuthorizationEvaluator
{
    Task<AuthorizationCheckResult> EvaluateAsync(
        AuthorizationCheckRequest request,
        CancellationToken cancellationToken = default);
}
