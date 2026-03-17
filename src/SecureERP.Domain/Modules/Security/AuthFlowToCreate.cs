namespace SecureERP.Domain.Modules.Security;

public sealed record AuthFlowToCreate(
    Guid AuthFlowId,
    long UserId,
    long TenantId,
    bool MfaRequired,
    bool MfaValidated,
    DateTime UtcExpiresAt,
    string? IpAddress,
    string? UserAgent,
    string? RequestId);
