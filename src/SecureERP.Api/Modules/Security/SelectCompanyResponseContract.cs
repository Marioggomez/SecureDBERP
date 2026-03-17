namespace SecureERP.Api.Modules.Security;

public sealed record SelectCompanyResponseContract(
    bool IsSuccess,
    string? AccessToken,
    Guid? SessionId,
    DateTimeOffset? ExpiresAtUtc,
    long? UserId,
    long? TenantId,
    long? CompanyId,
    string? ErrorCode,
    string? ErrorMessage);
