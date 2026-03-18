using SecureERP.Application.Modules.Organization.Abstractions;
using SecureERP.Application.Modules.Organization.DTOs;
using SecureERP.Domain.Modules.Organization;

namespace SecureERP.Application.Modules.Organization.Commands;

public sealed class CreateOrganizationUnitHandler : ICreateOrganizationUnitHandler
{
    private readonly IOrganizationPilotRepository _repository;

    public CreateOrganizationUnitHandler(IOrganizationPilotRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateOrganizationUnitResponse> HandleAsync(
        CreateOrganizationUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return CreateOrganizationUnitResponse.Failure("ORG_UNIT_CODE_REQUIRED", "Unit code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return CreateOrganizationUnitResponse.Failure("ORG_UNIT_NAME_REQUIRED", "Unit name is required.");
        }

        long id = await _repository.CreateUnitAsync(
            new OrganizationUnitToCreate(
                request.UnitTypeId,
                request.ParentUnitId,
                request.Code.Trim(),
                request.Name.Trim(),
                request.HierarchyLevel,
                request.HierarchyPath.Trim(),
                request.IsLeaf,
                request.IsActive,
                DateTime.UtcNow,
                null),
            cancellationToken);

        return CreateOrganizationUnitResponse.Success(id);
    }
}
