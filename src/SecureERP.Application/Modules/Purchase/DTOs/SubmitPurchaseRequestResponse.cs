namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record SubmitPurchaseRequestResponse(
    bool IsSuccess,
    short? NewStateId,
    string? NewStateCode,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static SubmitPurchaseRequestResponse Success(short newStateId, string newStateCode)
        => new(true, newStateId, newStateCode, null, null);

    public static SubmitPurchaseRequestResponse Failure(string errorCode, string errorMessage)
        => new(false, null, null, errorCode, errorMessage);
}
