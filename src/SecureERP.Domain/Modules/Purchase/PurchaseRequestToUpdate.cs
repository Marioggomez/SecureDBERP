namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseRequestToUpdate(
    long PurchaseRequestId,
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes,
    long UpdatedByUserId,
    DateTime UtcUpdatedAt);
