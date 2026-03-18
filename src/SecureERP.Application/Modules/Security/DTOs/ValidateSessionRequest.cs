namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record ValidateSessionRequest(
    string AccessToken,
    int IdleTimeoutMinutes = 30,
    bool UpdateLastActivity = true,
    string? IpAddress = null);
