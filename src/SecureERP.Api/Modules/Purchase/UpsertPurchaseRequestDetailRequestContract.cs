namespace SecureERP.Api.Modules.Purchase;

public sealed record UpsertPurchaseRequestDetailRequestContract(
    long? PurchaseRequestDetailId,
    int? LineNumber,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    string? CostCenterCode);
