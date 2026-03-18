namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record PurchaseRequestListItemDto(
    long PurchaseRequestId,
    string RequestNumber,
    DateTime RequestDate,
    short StateId,
    string StateCode,
    long CreatedByUserId,
    decimal EstimatedTotal,
    bool IsActive,
    DateTime UtcCreatedAt,
    DateTime? UtcUpdatedAt);
