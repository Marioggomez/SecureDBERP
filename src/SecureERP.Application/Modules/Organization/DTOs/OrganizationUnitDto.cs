namespace SecureERP.Application.Modules.Organization.DTOs;

public sealed record OrganizationUnitDto(
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
