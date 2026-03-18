namespace SecureERP.Api.Modules.Purchase;

public sealed record UpdatePurchaseOrderDraftResponseContract(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage);


