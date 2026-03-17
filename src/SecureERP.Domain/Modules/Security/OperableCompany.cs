namespace SecureERP.Domain.Modules.Security;

public sealed record OperableCompany(
    long CompanyId,
    string CompanyCode,
    string CompanyName,
    bool IsDefault);
