namespace SecureERP.Domain.Modules.Security;

public sealed record MfaChallengeSnapshot(
    Guid ChallengeId,
    Guid AuthFlowId,
    long UserId,
    long TenantId,
    long? CompanyId,
    byte[] OtpHash,
    byte[] OtpSalt,
    DateTime UtcExpiresAt,
    bool IsUsed,
    short Attempts,
    short MaxAttempts);
