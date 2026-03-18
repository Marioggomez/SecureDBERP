using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Services;

public sealed class OperationalSecurityService : IOperationalSecurityService
{
    private readonly IOperationalSecurityRepository _repository;

    public OperationalSecurityService(IOperationalSecurityRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationalSecurityDecision> GuardAsync(
        string actionCode,
        string? ipAddress,
        string? principalKey,
        long? tenantId,
        long? companyId,
        CancellationToken cancellationToken = default)
    {
        string safeIp = string.IsNullOrWhiteSpace(ipAddress) ? "0.0.0.0" : ipAddress.Trim();

        IpPolicyDecision ipDecision = await _repository.EvaluateIpPolicyAsync(
            tenantId,
            companyId,
            safeIp,
            cancellationToken);

        if (!ipDecision.IsAllowed)
        {
            return OperationalSecurityDecision.Deny(ipDecision.ReasonCode, 0);
        }

        RateLimitDecision ipRate = await _repository.EvaluateRateLimitAsync(
            actionCode,
            "IP",
            safeIp,
            tenantId,
            companyId,
            cancellationToken);

        if (!ipRate.IsAllowed)
        {
            return OperationalSecurityDecision.Deny("RATE_LIMIT_IP", ipRate.RetryAfterSeconds);
        }

        if (!string.IsNullOrWhiteSpace(principalKey))
        {
            RateLimitDecision principalRate = await _repository.EvaluateRateLimitAsync(
                actionCode,
                "PRINCIPAL",
                principalKey.Trim(),
                tenantId,
                companyId,
                cancellationToken);

            if (!principalRate.IsAllowed)
            {
                return OperationalSecurityDecision.Deny("RATE_LIMIT_PRINCIPAL", principalRate.RetryAfterSeconds);
            }
        }

        return OperationalSecurityDecision.Allow();
    }

    public async Task<OperationalLockoutDecision> CheckLoginLockoutAsync(
        string login,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        LoginLockoutDecision decision = await _repository.ControlLoginLockoutAsync(
            login,
            string.IsNullOrWhiteSpace(ipAddress) ? "0.0.0.0" : ipAddress.Trim(),
            "CHECK",
            cancellationToken);

        return new OperationalLockoutDecision(decision.IsLocked, decision.LockedUntilUtc, decision.Attempts);
    }

    public async Task<OperationalLockoutDecision> RegisterLoginFailureAsync(
        string login,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        LoginLockoutDecision decision = await _repository.ControlLoginLockoutAsync(
            login,
            string.IsNullOrWhiteSpace(ipAddress) ? "0.0.0.0" : ipAddress.Trim(),
            "FAILED",
            cancellationToken);

        return new OperationalLockoutDecision(decision.IsLocked, decision.LockedUntilUtc, decision.Attempts);
    }

    public async Task RegisterLoginSuccessAsync(
        string login,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await _repository.ControlLoginLockoutAsync(
            login,
            string.IsNullOrWhiteSpace(ipAddress) ? "0.0.0.0" : ipAddress.Trim(),
            "SUCCESS",
            cancellationToken);
    }
}
