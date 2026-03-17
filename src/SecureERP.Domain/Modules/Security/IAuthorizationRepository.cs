namespace SecureERP.Domain.Modules.Security;

public interface IAuthorizationRepository
{
    Task<AuthorizationDecision> EvaluateAsync(
        AuthorizationCheck request,
        CancellationToken cancellationToken = default);
}
