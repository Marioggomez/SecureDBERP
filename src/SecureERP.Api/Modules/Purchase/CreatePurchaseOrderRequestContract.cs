namespace SecureERP.Api.Modules.Purchase;

public sealed record CreatePurchaseOrderRequestContract(
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes);


