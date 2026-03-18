namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record SubmitPurchaseOrderRequest(
    long PurchaseOrderId,
    string? IpAddress = null,
    string? UserAgent = null);


