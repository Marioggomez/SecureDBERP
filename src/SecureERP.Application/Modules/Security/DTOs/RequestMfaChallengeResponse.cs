namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record RequestMfaChallengeResponse(
    bool IsSuccess,
    Guid? ChallengeId,
    DateTimeOffset? ExpiresAtUtc,
    string? DeliveryHint,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static RequestMfaChallengeResponse Success(Guid challengeId, DateTimeOffset expiresAtUtc, string? deliveryHint = null)
        => new(true, challengeId, expiresAtUtc, deliveryHint, null, null);

    public static RequestMfaChallengeResponse Failure(string code, string message)
        => new(false, null, null, null, code, message);
}
