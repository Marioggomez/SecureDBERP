namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record UpdatePurchaseOrderDraftResponse(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static UpdatePurchaseOrderDraftResponse Success()
        => new(true, null, null);

    public static UpdatePurchaseOrderDraftResponse Failure(string errorCode, string errorMessage)
        => new(false, errorCode, errorMessage);
}


