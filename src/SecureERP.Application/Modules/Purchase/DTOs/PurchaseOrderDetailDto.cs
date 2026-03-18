namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record PurchaseOrderDetailDto(
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


