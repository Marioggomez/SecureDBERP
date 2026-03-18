namespace SecureERP.Domain.Modules.Purchase;

public sealed record PurchaseRequestSnapshot(
    long PurchaseRequestId,
    long TenantId,
    long CompanyId,
    long? OrganizationUnitId,
    string RequestNumber,
    DateTime RequestDate,
    PurchaseRequestState State,
    long CreatedByUserId,
    DateTime UtcCreatedAt,
    long? UpdatedByUserId,
    DateTime? UtcUpdatedAt,
    string? Notes,
    decimal EstimatedTotal,
    bool IsActive,
    IReadOnlyList<PurchaseRequestDetailSnapshot> Details);
