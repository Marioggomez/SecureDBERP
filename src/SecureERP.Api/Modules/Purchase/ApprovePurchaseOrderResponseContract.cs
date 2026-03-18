namespace SecureERP.Api.Modules.Purchase;

public sealed record ApprovePurchaseOrderResponseContract(
    bool IsSuccess,
    short? NewStateId,
    string? NewStateCode,
    string? ErrorCode,
    string? ErrorMessage);


