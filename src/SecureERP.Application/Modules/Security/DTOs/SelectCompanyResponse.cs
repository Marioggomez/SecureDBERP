namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record SelectCompanyResponse(
    bool IsSuccess,
    string? AccessToken,
    Guid? SessionId,
    DateTimeOffset? ExpiresAtUtc,
    long? UserId,
    long? TenantId,
    long? CompanyId,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static SelectCompanyResponse Success(
        string accessToken,
        Guid sessionId,
        DateTimeOffset expiresAtUtc,
        long userId,
        long tenantId,
        long companyId)
    {
        return new SelectCompanyResponse(true, accessToken, sessionId, expiresAtUtc, userId, tenantId, companyId, null, null);
    }

    public static SelectCompanyResponse Failure(string errorCode, string errorMessage)
    {
        return new SelectCompanyResponse(false, null, null, null, null, null, null, errorCode, errorMessage);
    }
}
