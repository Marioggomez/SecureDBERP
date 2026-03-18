namespace SecureERP.Domain.Modules.Organization;

public sealed record OrganizationUnitSnapshot(
    long UnitId,
    long TenantId,
    long CompanyId,
    short UnitTypeId,
    long? ParentUnitId,
    string Code,
    string Name,
    short HierarchyLevel,
    string HierarchyPath,
    bool IsLeaf,
    bool IsActive,
    DateTime UtcCreatedAt,
    DateTime? UtcUpdatedAt);
