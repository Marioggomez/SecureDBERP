namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record SubmitPurchaseOrderResponse(
    bool IsSuccess,
    short? NewStateId,
    string? NewStateCode,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static SubmitPurchaseOrderResponse Success(short newStateId, string newStateCode)
        => new(true, newStateId, newStateCode, null, null);

    public static SubmitPurchaseOrderResponse Failure(string errorCode, string errorMessage)
        => new(false, null, null, errorCode, errorMessage);
}


