namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record CreatePurchaseRequestResponse(
    bool IsSuccess,
    long? PurchaseRequestId,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static CreatePurchaseRequestResponse Success(long purchaseRequestId)
        => new(true, purchaseRequestId, null, null);

    public static CreatePurchaseRequestResponse Failure(string errorCode, string errorMessage)
        => new(false, null, errorCode, errorMessage);
}
