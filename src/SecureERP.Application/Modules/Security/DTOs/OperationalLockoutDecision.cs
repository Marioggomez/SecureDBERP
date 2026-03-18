namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record OperationalLockoutDecision(
    bool IsLocked,
    DateTime? LockedUntilUtc,
    int Attempts);
