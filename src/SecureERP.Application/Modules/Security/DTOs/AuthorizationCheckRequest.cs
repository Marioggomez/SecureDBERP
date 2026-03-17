namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record AuthorizationCheckRequest(
    string PermissionCode,
    bool RequiresMfa);
