namespace SecureERP.Api.Modules.Security;

public sealed record OperableCompanyContract(
    long CompanyId,
    string CompanyCode,
    string CompanyName,
    bool IsDefault);
