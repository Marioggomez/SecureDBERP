namespace SecureERP.Api.Modules.Purchase;

public sealed record UpsertPurchaseOrderDetailRequestContract(
    long? PurchaseOrderDetailId,
    int? LineNumber,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    string? CostCenterCode);


