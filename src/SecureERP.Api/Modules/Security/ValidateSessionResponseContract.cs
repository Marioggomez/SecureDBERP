namespace SecureERP.Api.Modules.Security;

public sealed record ValidateSessionResponseContract(
    bool IsValid,
    Guid? SessionId,
    long? UserId,
    long? TenantId,
    long? CompanyId,
    bool MfaValidated,
    DateTimeOffset? ExpiresAtUtc,
    string? ErrorCode,
    string? ErrorMessage);
