namespace SecureERP.Api.Modules.Purchase;

public sealed record PurchaseRequestDetailContract(
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
