namespace SecureERP.Application.Modules.Security.Abstractions;

using SecureERP.Application.Modules.Security.DTOs;

public interface IOperationalSecurityService
{
    Task<OperationalSecurityDecision> GuardAsync(
        string actionCode,
        string? ipAddress,
        string? principalKey,
        long? tenantId,
        long? companyId,
        CancellationToken cancellationToken = default);

    Task<OperationalLockoutDecision> CheckLoginLockoutAsync(
        string login,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<OperationalLockoutDecision> RegisterLoginFailureAsync(
        string login,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task RegisterLoginSuccessAsync(
        string login,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
