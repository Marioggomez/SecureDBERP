namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record LoginResponse(
    bool IsAuthenticated,
    string? AccessToken,
    Guid? SessionId,
    DateTimeOffset? ExpiresAtUtc,
    long? UserId,
    long? TenantId,
    long? CompanyId,
    bool RequiresPasswordChange,
    bool RequiresMfa,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static LoginResponse Success(
        string accessToken,
        Guid sessionId,
        DateTimeOffset expiresAtUtc,
        long userId,
        long tenantId,
        long companyId,
        bool requiresPasswordChange)
    {
        return new LoginResponse(
            true,
            accessToken,
            sessionId,
            expiresAtUtc,
            userId,
            tenantId,
            companyId,
            requiresPasswordChange,
            false,
            null,
            null);
    }

    public static LoginResponse Failure(string errorCode, string errorMessage, bool requiresMfa = false)
    {
        return new LoginResponse(
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            requiresMfa,
            errorCode,
            errorMessage);
    }
}
