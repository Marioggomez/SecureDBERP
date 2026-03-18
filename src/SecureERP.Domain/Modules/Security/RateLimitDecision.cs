namespace SecureERP.Domain.Modules.Security;

public sealed record RateLimitDecision(
    bool IsAllowed,
    int Count,
    int MaxAttempts,
    int RetryAfterSeconds);
