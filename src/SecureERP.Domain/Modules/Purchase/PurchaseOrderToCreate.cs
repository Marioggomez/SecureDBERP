namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseOrderToCreate(
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes,
    long CreatedByUserId,
    DateTime UtcCreatedAt);


