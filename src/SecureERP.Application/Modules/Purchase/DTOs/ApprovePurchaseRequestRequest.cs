namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record ApprovePurchaseRequestRequest(
    long PurchaseRequestId,
    string? Comment,
    string? IpAddress = null,
    string? UserAgent = null);
