namespace SecureERP.Domain.Modules.Security;

public sealed record MfaChallengeToCreate(
    Guid ChallengeId,
    long UserId,
    long TenantId,
    long? CompanyId,
    Guid? SessionId,
    Guid? AuthFlowId,
    MfaPurpose Purpose,
    MfaChannel Channel,
    string? ActionCode,
    byte[] OtpHash,
    byte[] OtpSalt,
    DateTime UtcExpiresAt,
    short MaxAttempts);
