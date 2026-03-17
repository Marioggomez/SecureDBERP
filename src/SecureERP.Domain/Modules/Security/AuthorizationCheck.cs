namespace SecureERP.Domain.Modules.Security;

public sealed record AuthorizationCheck(
    long UserId,
    long TenantId,
    long CompanyId,
    Guid SessionId,
    string PermissionCode,
    bool RequiresMfa);
