namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record SelectCompanyRequest(
    Guid AuthFlowId,
    long CompanyId,
    string? IpAddress = null,
    string? UserAgent = null);
