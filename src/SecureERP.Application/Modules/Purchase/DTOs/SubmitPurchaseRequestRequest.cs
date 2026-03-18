namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record SubmitPurchaseRequestRequest(
    long PurchaseRequestId,
    string? IpAddress = null,
    string? UserAgent = null);
