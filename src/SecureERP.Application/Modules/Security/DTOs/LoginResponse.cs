namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record LoginResponse(
    bool IsAuthenticated,
    Guid? AuthFlowId,
    long? UserId,
    long? TenantId,
    IReadOnlyList<OperableCompanyDto> OperableCompanies,
    bool RequiresPasswordChange,
    bool RequiresMfa,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static LoginResponse Success(
        Guid authFlowId,
        long userId,
        long tenantId,
        IReadOnlyList<OperableCompanyDto> operableCompanies,
        bool requiresPasswordChange,
        bool requiresMfa)
    {
        return new LoginResponse(
            true,
            authFlowId,
            userId,
            tenantId,
            operableCompanies,
            requiresPasswordChange,
            requiresMfa,
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
            Array.Empty<OperableCompanyDto>(),
            false,
            requiresMfa,
            errorCode,
            errorMessage);
    }
}
