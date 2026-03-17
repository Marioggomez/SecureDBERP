namespace SecureERP.Domain.Modules.Security;

public sealed record AuthorizationAuditEntry(
    DateTime UtcTimestamp,
    long TenantId,
    long UserId,
    long CompanyId,
    Guid SessionId,
    string PermissionCode,
    string OperationCode,
    string HttpMethod,
    bool Allowed,
    string Reason,
    string? EntityCode,
    long? ObjectId,
    string? IpAddress,
    string? UserAgent,
    string? RequestId);
