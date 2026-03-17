namespace SecureERP.Domain.Modules.Security;

public sealed record UserSessionToCreate(
    Guid SessionId,
    long UserId,
    long TenantId,
    long CompanyId,
    byte[] TokenHash,
    string AuthenticationSource,
    bool MfaValidated,
    DateTime UtcCreatedAt,
    DateTime UtcExpiresAt,
    DateTime UtcLastActivityAt,
    string? IpAddress,
    string? UserAgent);
