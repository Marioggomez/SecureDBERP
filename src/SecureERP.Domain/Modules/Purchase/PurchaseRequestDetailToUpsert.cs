namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseRequestDetailToUpsert(
    long PurchaseRequestId,
    long? PurchaseRequestDetailId,
    int? LineNumber,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    string? CostCenterCode,
    long UpdatedByUserId,
    DateTime UtcUpdatedAt);
