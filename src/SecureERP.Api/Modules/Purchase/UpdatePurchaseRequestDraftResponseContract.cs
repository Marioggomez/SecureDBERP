namespace SecureERP.Api.Modules.Purchase;

public sealed record UpdatePurchaseRequestDraftResponseContract(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage);
