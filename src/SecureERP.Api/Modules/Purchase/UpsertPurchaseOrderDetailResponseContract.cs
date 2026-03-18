namespace SecureERP.Api.Modules.Purchase;

public sealed record UpsertPurchaseOrderDetailResponseContract(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage);


