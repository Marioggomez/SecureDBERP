namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record CreatePurchaseOrderResponse(
    bool IsSuccess,
    long? PurchaseOrderId,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static CreatePurchaseOrderResponse Success(long purchaseOrderId)
        => new(true, purchaseOrderId, null, null);

    public static CreatePurchaseOrderResponse Failure(string errorCode, string errorMessage)
        => new(false, null, errorCode, errorMessage);
}


