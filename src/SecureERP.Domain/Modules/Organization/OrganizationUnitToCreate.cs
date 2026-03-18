namespace SecureERP.Domain.Modules.Organization;

public sealed record OrganizationUnitToCreate(
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
