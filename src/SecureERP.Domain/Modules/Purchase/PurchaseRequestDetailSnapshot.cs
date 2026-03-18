namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseRequestDetailSnapshot(
    long PurchaseRequestDetailId,
    long PurchaseRequestId,
    int LineNumber,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    decimal EstimatedTotal,
    string? CostCenterCode,
    bool IsActive,
    DateTime UtcCreatedAt,
    DateTime? UtcUpdatedAt);
