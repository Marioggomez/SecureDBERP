namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record OperationalSecurityDecision(
    bool IsAllowed,
    string Code,
    int RetryAfterSeconds)
{
    public static OperationalSecurityDecision Allow() => new(true, "OK", 0);

    public static OperationalSecurityDecision Deny(string code, int retryAfterSeconds = 0) =>
        new(false, code, retryAfterSeconds);
}
