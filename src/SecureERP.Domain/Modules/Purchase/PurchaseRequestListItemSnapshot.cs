namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseRequestListItemSnapshot(
    long PurchaseRequestId,
    string RequestNumber,
    DateTime RequestDate,
    PurchaseRequestState State,
    long CreatedByUserId,
    decimal EstimatedTotal,
    bool IsActive,
    DateTime UtcCreatedAt,
    DateTime? UtcUpdatedAt);
