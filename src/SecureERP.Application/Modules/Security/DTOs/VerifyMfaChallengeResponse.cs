namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record VerifyMfaChallengeResponse(
    bool IsVerified,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static VerifyMfaChallengeResponse Success() => new(true, null, null);

    public static VerifyMfaChallengeResponse Failure(string code, string message) => new(false, code, message);
}
