namespace SecureERP.Api.Modules.Purchase;

public sealed record UpdatePurchaseOrderDraftRequestContract(
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes);


