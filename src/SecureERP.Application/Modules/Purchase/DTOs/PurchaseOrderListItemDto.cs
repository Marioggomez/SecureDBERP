namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record PurchaseOrderListItemDto(
    long PurchaseOrderId,
    string RequestNumber,
    DateTime RequestDate,
    short StateId,
    string StateCode,
    long CreatedByUserId,
    decimal EstimatedTotal,
    bool IsActive,
    DateTime UtcCreatedAt,
    DateTime? UtcUpdatedAt);


