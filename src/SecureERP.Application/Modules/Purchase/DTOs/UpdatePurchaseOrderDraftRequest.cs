namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record UpdatePurchaseOrderDraftRequest(
    long PurchaseOrderId,
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes,
    string? IpAddress = null,
    string? UserAgent = null);


