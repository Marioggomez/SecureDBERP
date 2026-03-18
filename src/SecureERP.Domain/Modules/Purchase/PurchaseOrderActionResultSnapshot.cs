namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseOrderActionResultSnapshot(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    PurchaseOrderState? NewState);


