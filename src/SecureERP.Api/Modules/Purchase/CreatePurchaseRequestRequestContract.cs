namespace SecureERP.Api.Modules.Purchase;

public sealed record CreatePurchaseRequestRequestContract(
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes);
