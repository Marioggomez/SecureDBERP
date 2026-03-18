using Microsoft.AspNetCore.Mvc;
using SecureERP.Api.Modules.Security;
using SecureERP.Application.Modules.Organization.Abstractions;
using SecureERP.Application.Modules.Organization.DTOs;

namespace SecureERP.Api.Modules.Organization;

[ApiController]
[Route("api/v1/organization/units")]
public sealed class OrganizationUnitsController : ControllerBase
{
    private readonly IListOrganizationUnitsHandler _listHandler;
    private readonly ICreateOrganizationUnitHandler _createHandler;

    public OrganizationUnitsController(
        IListOrganizationUnitsHandler listHandler,
        ICreateOrganizationUnitHandler createHandler)
    {
        _listHandler = listHandler;
        _createHandler = createHandler;
    }

    [HttpGet]
    [RequirePermission("ORGANIZATION.UNIT.READ")]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationUnitContract>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrganizationUnitContract>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<OrganizationUnitDto> data = await _listHandler.HandleAsync(cancellationToken);
        return Ok(data.Select(item => new OrganizationUnitContract(
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
            item.UtcUpdatedAt)).ToList());
    }

    [HttpPost]
    [RequirePermission("ORGANIZATION.UNIT.CREATE")]
    [ProducesResponseType(typeof(CreateOrganizationUnitResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CreateOrganizationUnitResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateOrganizationUnitResponseContract>> Create(
        [FromBody] CreateOrganizationUnitRequestContract request,
        CancellationToken cancellationToken)
    {
        CreateOrganizationUnitResponse result = await _createHandler.HandleAsync(
            new CreateOrganizationUnitRequest(
                request.UnitTypeId,
                request.ParentUnitId,
                request.Code,
                request.Name,
                request.HierarchyLevel,
                request.HierarchyPath,
                request.IsLeaf,
                request.IsActive),
            cancellationToken);

        CreateOrganizationUnitResponseContract response = new(
            result.IsSuccess,
            result.UnitId,
            result.ErrorCode,
            result.ErrorMessage);

        if (!result.IsSuccess)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
