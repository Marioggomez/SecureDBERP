namespace SecureERP.Application.Modules.Purchase.DTOs;

public sealed record PurchaseOrderDto(
    long PurchaseOrderId,
    long TenantId,
    long CompanyId,
    long? OrganizationUnitId,
    string RequestNumber,
    DateTime RequestDate,
    short StateId,
    string StateCode,
    long CreatedByUserId,
    DateTime UtcCreatedAt,
    long? UpdatedByUserId,
    DateTime? UtcUpdatedAt,
    string? Notes,
    decimal EstimatedTotal,
    bool IsActive,
    IReadOnlyList<PurchaseOrderDetailDto> Details);


