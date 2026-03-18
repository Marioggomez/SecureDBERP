namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record ApprovePurchaseOrderResponse(
    bool IsSuccess,
    short? NewStateId,
    string? NewStateCode,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static ApprovePurchaseOrderResponse Success(short newStateId, string newStateCode)
        => new(true, newStateId, newStateCode, null, null);

    public static ApprovePurchaseOrderResponse Failure(string errorCode, string errorMessage)
        => new(false, null, null, errorCode, errorMessage);
}


