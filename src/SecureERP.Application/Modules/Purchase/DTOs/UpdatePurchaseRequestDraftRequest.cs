namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record UpdatePurchaseRequestDraftRequest(
    long PurchaseRequestId,
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes,
    string? IpAddress = null,
    string? UserAgent = null);
