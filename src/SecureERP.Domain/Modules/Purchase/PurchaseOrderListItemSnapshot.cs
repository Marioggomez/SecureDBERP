namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseOrderListItemSnapshot(
    long PurchaseOrderId,
    string RequestNumber,
    DateTime RequestDate,
    PurchaseOrderState State,
    long CreatedByUserId,
    decimal EstimatedTotal,
    bool IsActive,
    DateTime UtcCreatedAt,
    DateTime? UtcUpdatedAt);


