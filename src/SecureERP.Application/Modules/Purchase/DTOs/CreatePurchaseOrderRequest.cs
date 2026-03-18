namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record CreatePurchaseOrderRequest(
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes,
    string? IpAddress = null,
    string? UserAgent = null);


