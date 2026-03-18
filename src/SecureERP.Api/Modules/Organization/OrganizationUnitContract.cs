namespace SecureERP.Api.Modules.Organization;

public sealed record OrganizationUnitContract(
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
