namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseRequestToCreate(
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes,
    long CreatedByUserId,
    DateTime UtcCreatedAt);
