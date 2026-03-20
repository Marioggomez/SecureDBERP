namespace SecureERP.Api.Modules.Security;

public sealed record RevokeSessionResponseContract(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage);
