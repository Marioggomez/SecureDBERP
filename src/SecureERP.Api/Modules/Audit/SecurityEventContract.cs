namespace SecureERP.Api.Modules.Audit;

public sealed record SecurityEventContract(
    long SecurityEventId,
    DateTime UtcCreatedAt,
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
