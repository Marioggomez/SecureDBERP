namespace SecureERP.Api.Modules.Purchase;

public sealed record CreatePurchaseOrderResponseContract(
    bool IsSuccess,
    long? PurchaseOrderId,
    string? ErrorCode,
    string? ErrorMessage);


