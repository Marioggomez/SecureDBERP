namespace SecureERP.Api.Modules.Security;

public sealed record LoginResponseContract(
    bool IsAuthenticated,
    Guid? AuthFlowId,
    long? UserId,
    long? TenantId,
    IReadOnlyList<OperableCompanyContract> OperableCompanies,
    bool RequiresPasswordChange,
    bool RequiresMfa,
    string? ErrorCode,
    string? ErrorMessage);
