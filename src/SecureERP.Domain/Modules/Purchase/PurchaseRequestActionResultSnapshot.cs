namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseRequestActionResultSnapshot(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    PurchaseRequestState? NewState);
