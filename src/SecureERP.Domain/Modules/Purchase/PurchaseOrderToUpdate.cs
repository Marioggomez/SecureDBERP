namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseOrderToUpdate(
    long PurchaseOrderId,
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes,
    long UpdatedByUserId,
    DateTime UtcUpdatedAt);


