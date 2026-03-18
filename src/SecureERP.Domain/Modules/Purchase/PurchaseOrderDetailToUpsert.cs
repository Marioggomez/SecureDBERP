namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseOrderDetailToUpsert(
    long PurchaseOrderId,
    long? PurchaseOrderDetailId,
    int? LineNumber,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    string? CostCenterCode,
    long UpdatedByUserId,
    DateTime UtcUpdatedAt);


