namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record UpsertPurchaseRequestDetailRequest(
    long PurchaseRequestId,
    long? PurchaseRequestDetailId,
    int? LineNumber,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    string? CostCenterCode,
    string? IpAddress = null,
    string? UserAgent = null);
