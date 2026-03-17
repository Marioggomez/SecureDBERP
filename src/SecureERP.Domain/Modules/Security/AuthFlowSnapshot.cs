namespace SecureERP.Domain.Modules.Security;

public sealed record AuthFlowSnapshot(
    Guid AuthFlowId,
    long UserId,
    long TenantId,
    bool MfaRequired,
    bool MfaValidated,
    bool IsUsed,
    DateTime UtcExpiresAt);
