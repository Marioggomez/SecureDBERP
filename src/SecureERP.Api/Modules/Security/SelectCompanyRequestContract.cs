namespace SecureERP.Api.Modules.Security;

public sealed record SelectCompanyRequestContract(
    Guid AuthFlowId,
    long CompanyId);
