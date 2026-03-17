namespace SecureERP.Domain.Modules.Security;

public sealed record MfaChallengeToCreate(
    Guid ChallengeId,
    long UserId,
    long TenantId,
    long CompanyId,
    Guid AuthFlowId,
    MfaPurpose Purpose,
    MfaChannel Channel,
    string? ActionCode,
    byte[] OtpHash,
    byte[] OtpSalt,
    DateTime UtcExpiresAt,
    short MaxAttempts);
