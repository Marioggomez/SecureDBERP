namespace SecureERP.Api.Modules.Organization;

public sealed record CreateOrganizationUnitRequestContract(
    short UnitTypeId,
    long? ParentUnitId,
    string Code,
    string Name,
    short HierarchyLevel,
    string HierarchyPath,
    bool IsLeaf,
    bool IsActive = true);
