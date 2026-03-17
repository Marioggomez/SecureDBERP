namespace SecureERP.Domain.Modules.Security;

public sealed record SessionValidationSnapshot(
    Guid SessionId,
    long UserId,
    long TenantId,
    long CompanyId,
    bool MfaValidated,
    DateTime UtcAbsoluteExpiration,
    DateTime UtcLastActivity,
    bool IsSessionValid);
