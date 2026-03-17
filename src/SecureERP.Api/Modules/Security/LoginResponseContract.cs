namespace SecureERP.Api.Modules.Security;

public sealed record LoginResponseContract(
    bool IsAuthenticated,
    string? AccessToken,
    Guid? SessionId,
    DateTimeOffset? ExpiresAtUtc,
    long? UserId,
    long? TenantId,
    long? CompanyId,
    bool RequiresPasswordChange,
    bool RequiresMfa,
    string? ErrorCode,
    string? ErrorMessage);
