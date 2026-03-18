namespace SecureERP.Api.Modules.Purchase;

public sealed record ApprovePurchaseRequestResponseContract(
    bool IsSuccess,
    short? NewStateId,
    string? NewStateCode,
    string? ErrorCode,
    string? ErrorMessage);
