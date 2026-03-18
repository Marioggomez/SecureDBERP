namespace SecureERP.Domain.Modules.Security;

public interface IOperationalSecurityRepository
{
    Task<RateLimitDecision> EvaluateRateLimitAsync(
        string actionCode,
        string scope,
        string key,
        long? tenantId,
        long? companyId,
        CancellationToken cancellationToken = default);

    Task<IpPolicyDecision> EvaluateIpPolicyAsync(
        long? tenantId,
        long? companyId,
        string ipAddress,
        CancellationToken cancellationToken = default);

    Task<LoginLockoutDecision> ControlLoginLockoutAsync(
        string login,
        string ipAddress,
        string mode,
        CancellationToken cancellationToken = default);
}
