using SecureERP.Application.Modules.Organization.Abstractions;
using SecureERP.Application.Modules.Organization.DTOs;
using SecureERP.Domain.Modules.Organization;

namespace SecureERP.Application.Modules.Organization.Queries;

public sealed class ListOrganizationUnitsHandler : IListOrganizationUnitsHandler
{
    private readonly IOrganizationPilotRepository _repository;

    public ListOrganizationUnitsHandler(IOrganizationPilotRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<OrganizationUnitDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrganizationUnitSnapshot> data = await _repository.ListUnitsAsync(cancellationToken);
        return data.Select(item => new OrganizationUnitDto(
            item.UnitId,
            item.TenantId,
            item.CompanyId,
            item.UnitTypeId,
            item.ParentUnitId,
            item.Code,
            item.Name,
            item.HierarchyLevel,
            item.HierarchyPath,
            item.IsLeaf,
            item.IsActive,
            item.UtcCreatedAt,
            item.UtcUpdatedAt)).ToList();
    }
}
