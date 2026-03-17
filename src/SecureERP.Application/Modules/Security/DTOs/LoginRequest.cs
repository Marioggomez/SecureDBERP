namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record LoginRequest(
    string TenantCode,
    string Identifier,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null);
