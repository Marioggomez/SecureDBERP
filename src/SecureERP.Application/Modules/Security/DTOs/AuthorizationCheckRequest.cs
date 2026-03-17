namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record AuthorizationCheckRequest(
    string PermissionCode,
    bool RequiresMfa,
    string OperationCode,
    string HttpMethod,
    string? IpAddress,
    string? UserAgent,
    string? RequestId);
