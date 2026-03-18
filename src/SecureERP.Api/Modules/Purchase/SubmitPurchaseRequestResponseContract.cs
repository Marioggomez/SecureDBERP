namespace SecureERP.Api.Modules.Purchase;

public sealed record SubmitPurchaseRequestResponseContract(
    bool IsSuccess,
    short? NewStateId,
    string? NewStateCode,
    string? ErrorCode,
    string? ErrorMessage);
