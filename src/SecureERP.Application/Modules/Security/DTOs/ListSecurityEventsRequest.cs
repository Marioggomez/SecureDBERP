namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record ListSecurityEventsRequest(
    int Top,
    string? EventType,
    string? Severity,
    string? Result);
