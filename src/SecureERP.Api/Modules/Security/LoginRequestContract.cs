namespace SecureERP.Api.Modules.Security;

public sealed record LoginRequestContract(
    string TenantCode,
    string Identifier,
    string Password);
