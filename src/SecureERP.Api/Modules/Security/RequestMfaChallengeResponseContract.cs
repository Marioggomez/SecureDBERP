namespace SecureERP.Api.Modules.Security;

public sealed record RequestMfaChallengeResponseContract(
    bool IsSuccess,
    Guid? ChallengeId,
    DateTimeOffset? ExpiresAtUtc,
    string? DeliveryHint,
    string? ErrorCode,
    string? ErrorMessage);
