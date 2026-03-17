namespace SecureERP.Domain.Modules.Security;

public sealed record SecurityEventToCreate(
    string EventType,
    string Severity,
    string Result,
    string? Detail,
    long? TenantId,
    long? CompanyId,
    long? UserId,
    Guid? SessionId,
    Guid? AuthFlowId,
    Guid? CorrelationId,
    string? IpAddress,
    string? UserAgent);
