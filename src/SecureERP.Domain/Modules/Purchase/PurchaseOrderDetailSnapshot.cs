namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseOrderDetailSnapshot(
    long PurchaseOrderDetailId,
    long PurchaseOrderId,
    int LineNumber,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    decimal EstimatedTotal,
    string? CostCenterCode,
    bool IsActive,
    DateTime UtcCreatedAt,
    DateTime? UtcUpdatedAt);


