namespace SecureERP.Api.Modules.Organization;

public sealed record CreateOrganizationUnitResponseContract(
    bool IsSuccess,
    long? UnitId,
    string? ErrorCode,
    string? ErrorMessage);
