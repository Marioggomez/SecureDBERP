namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record RevokeSessionRequest(
    Guid SessionId,
    string? Reason,
    string? IpAddress,
    string? UserAgent);
