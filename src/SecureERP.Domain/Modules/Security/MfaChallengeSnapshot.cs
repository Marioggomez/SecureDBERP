namespace SecureERP.Domain.Modules.Security;

public sealed record MfaChallengeSnapshot(
    Guid ChallengeId,
    Guid? AuthFlowId,
    Guid? SessionId,
    long UserId,
    long TenantId,
    long? CompanyId,
    MfaPurpose Purpose,
    byte[] OtpHash,
    byte[] OtpSalt,
    DateTime UtcExpiresAt,
    bool IsUsed,
    short Attempts,
    short MaxAttempts);
