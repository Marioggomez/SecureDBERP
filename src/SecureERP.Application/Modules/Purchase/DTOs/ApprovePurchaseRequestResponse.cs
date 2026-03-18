namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record ApprovePurchaseRequestResponse(
    bool IsSuccess,
    short? NewStateId,
    string? NewStateCode,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static ApprovePurchaseRequestResponse Success(short newStateId, string newStateCode)
        => new(true, newStateId, newStateCode, null, null);

    public static ApprovePurchaseRequestResponse Failure(string errorCode, string errorMessage)
        => new(false, null, null, errorCode, errorMessage);
}
