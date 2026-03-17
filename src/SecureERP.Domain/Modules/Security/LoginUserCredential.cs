namespace SecureERP.Domain.Modules.Security;

public sealed record LoginUserCredential(
    long UserId,
    long TenantId,
    long CompanyId,
    string TenantCode,
    string LoginPrincipal,
    string DisplayName,
    string? Email,
    bool MfaEnabled,
    bool RequiresPasswordChange,
    int UserStatusId,
    bool IsActiveUser,
    byte[] PasswordHash,
    byte[] PasswordSalt,
    string PasswordAlgorithm,
    int PasswordIterations,
    bool IsCredentialActive);
