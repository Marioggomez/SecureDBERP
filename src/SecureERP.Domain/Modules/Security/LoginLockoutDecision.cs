namespace SecureERP.Domain.Modules.Security;

public sealed record LoginLockoutDecision(
    bool IsLocked,
    DateTime? LockedUntilUtc,
    int Attempts);
