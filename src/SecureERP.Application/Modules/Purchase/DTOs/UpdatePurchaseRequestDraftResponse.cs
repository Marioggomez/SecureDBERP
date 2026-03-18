namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record UpdatePurchaseRequestDraftResponse(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static UpdatePurchaseRequestDraftResponse Success()
        => new(true, null, null);

    public static UpdatePurchaseRequestDraftResponse Failure(string errorCode, string errorMessage)
        => new(false, errorCode, errorMessage);
}
