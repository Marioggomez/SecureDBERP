namespace SecureERP.Api.Modules.Purchase;

public sealed record PurchaseOrderDetailContract(
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


