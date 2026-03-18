namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record ApprovePurchaseOrderRequest(
    long PurchaseOrderId,
    string? Comment,
    string? IpAddress = null,
    string? UserAgent = null);


