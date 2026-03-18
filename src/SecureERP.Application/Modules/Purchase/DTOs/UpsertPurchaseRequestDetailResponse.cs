namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record UpsertPurchaseRequestDetailResponse(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static UpsertPurchaseRequestDetailResponse Success()
        => new(true, null, null);

    public static UpsertPurchaseRequestDetailResponse Failure(string errorCode, string errorMessage)
        => new(false, errorCode, errorMessage);
}
