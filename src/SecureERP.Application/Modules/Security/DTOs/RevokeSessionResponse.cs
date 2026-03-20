namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record RevokeSessionResponse(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage);
