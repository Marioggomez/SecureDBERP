namespace SecureERP.Application.Modules.Organization.DTOs;

public sealed record CreateOrganizationUnitRequest(
    short UnitTypeId,
    long? ParentUnitId,
    string Code,
    string Name,
    short HierarchyLevel,
    string HierarchyPath,
    bool IsLeaf,
    bool IsActive = true);
