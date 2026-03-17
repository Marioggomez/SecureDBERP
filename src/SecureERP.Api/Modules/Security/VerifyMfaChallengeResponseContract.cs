namespace SecureERP.Api.Modules.Security;

public sealed record VerifyMfaChallengeResponseContract(
    bool IsVerified,
    string? ErrorCode,
    string? ErrorMessage);
