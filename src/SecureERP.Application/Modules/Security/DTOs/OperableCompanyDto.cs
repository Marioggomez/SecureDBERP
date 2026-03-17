namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record OperableCompanyDto(
    long CompanyId,
    string CompanyCode,
    string CompanyName,
    bool IsDefault);
