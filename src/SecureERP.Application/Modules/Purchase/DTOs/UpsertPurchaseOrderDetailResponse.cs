namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record UpsertPurchaseOrderDetailResponse(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static UpsertPurchaseOrderDetailResponse Success()
        => new(true, null, null);

    public static UpsertPurchaseOrderDetailResponse Failure(string errorCode, string errorMessage)
        => new(false, errorCode, errorMessage);
}


