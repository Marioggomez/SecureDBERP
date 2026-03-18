using SecureERP.Application.Modules.Organization.DTOs;

namespace SecureERP.Application.Modules.Organization.Abstractions;

public interface ICreateOrganizationUnitHandler
{
    Task<CreateOrganizationUnitResponse> HandleAsync(
        CreateOrganizationUnitRequest request,
        CancellationToken cancellationToken = default);
}
