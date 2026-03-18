namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record UpsertPurchaseOrderDetailRequest(
    long PurchaseOrderId,
    long? PurchaseOrderDetailId,
    int? LineNumber,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    string? CostCenterCode,
    string? IpAddress = null,
    string? UserAgent = null);


