using SecureERP.Application.Modules.Organization.DTOs;

namespace SecureERP.Application.Modules.Organization.Abstractions;

public interface IListOrganizationUnitsHandler
{
    Task<IReadOnlyList<OrganizationUnitDto>> HandleAsync(CancellationToken cancellationToken = default);
}
