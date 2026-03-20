namespace SecureERP.Api.Modules.Security;

public sealed record SecurityUserContract(
    long UserId,
    string Code,
    string Login,
    string DisplayName,
    string? Email,
    bool MfaEnabled,
    bool RequiresPasswordChange,
    bool IsActive,
    bool IsTenantAdministrator,
    long? CompanyId,
    bool IsDefaultCompany,
    bool CanOperateCompany,
    DateTime? CompanyScopeStartUtc,
    DateTime? CompanyScopeEndUtc,
    DateTime? BlockedUntilUtc,
    DateTime? LastAccessUtc);
