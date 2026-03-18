namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record PurchaseRequestDetailDto(
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
