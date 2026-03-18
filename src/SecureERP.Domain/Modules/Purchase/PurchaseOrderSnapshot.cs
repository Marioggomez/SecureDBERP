namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseOrderSnapshot(
    long PurchaseOrderId,
    long TenantId,
    long CompanyId,
    long? OrganizationUnitId,
    string RequestNumber,
    DateTime RequestDate,
    PurchaseOrderState State,
    long CreatedByUserId,
    DateTime UtcCreatedAt,
    long? UpdatedByUserId,
    DateTime? UtcUpdatedAt,
    string? Notes,
    decimal EstimatedTotal,
    bool IsActive,
    IReadOnlyList<PurchaseOrderDetailSnapshot> Details);


