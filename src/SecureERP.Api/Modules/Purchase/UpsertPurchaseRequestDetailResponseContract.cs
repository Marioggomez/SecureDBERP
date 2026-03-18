namespace SecureERP.Api.Modules.Purchase;

public sealed record UpsertPurchaseRequestDetailResponseContract(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage);
