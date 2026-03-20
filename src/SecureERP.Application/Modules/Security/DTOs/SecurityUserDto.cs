namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record SecurityUserDto(
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
