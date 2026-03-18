namespace SecureERP.Api.Modules.Purchase;

public sealed record PurchaseOrderListItemContract(
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


