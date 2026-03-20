namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record SecurityEventDto(
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
