namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record ValidateSessionResult(
    bool IsValid,
    Guid? SessionId,
    long? UserId,
    long? TenantId,
    long? CompanyId,
    bool MfaValidated,
    DateTimeOffset? ExpiresAtUtc,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static ValidateSessionResult Success(
        Guid sessionId,
        long userId,
        long tenantId,
        long companyId,
        bool mfaValidated,
        DateTimeOffset expiresAtUtc)
    {
        return new ValidateSessionResult(
            true,
            sessionId,
            userId,
            tenantId,
            companyId,
            mfaValidated,
            expiresAtUtc,
            null,
            null);
    }

    public static ValidateSessionResult Failure(string errorCode, string errorMessage)
    {
        return new ValidateSessionResult(false, null, null, null, null, false, null, errorCode, errorMessage);
    }
}
