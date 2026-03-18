namespace SecureERP.Api.Modules.Purchase;

public sealed record CreatePurchaseRequestResponseContract(
    bool IsSuccess,
    long? PurchaseRequestId,
    string? ErrorCode,
    string? ErrorMessage);
