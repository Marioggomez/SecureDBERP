using Microsoft.AspNetCore.Mvc;
using SecureERP.Api.Modules.Security;
using SecureERP.Application.Modules.Workflow.Abstractions;
using SecureERP.Application.Modules.Workflow.DTOs;
using SecureERP.Application.Modules.Security;

namespace SecureERP.Api.Modules.Workflow;

[ApiController]
[Route("api/v1/workflow/approval-instances")]
public sealed class ApprovalInstancesController : ControllerBase
{
    private readonly IListApprovalInstancesHandler _listHandler;
    private readonly ICreateApprovalInstanceHandler _createHandler;

    public ApprovalInstancesController(
        IListApprovalInstancesHandler listHandler,
        ICreateApprovalInstanceHandler createHandler)
    {
        _listHandler = listHandler;
        _createHandler = createHandler;
    }

    [HttpGet]
    [RequirePermission(Permissions.WorkflowApprovalInstanceRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ApprovalInstanceContract>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApprovalInstanceContract>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<ApprovalInstanceDto> data = await _listHandler.HandleAsync(cancellationToken);
        return Ok(data.Select(item => new ApprovalInstanceContract(
            item.ApprovalInstanceId,
            item.TenantId,
            item.CompanyId,
            item.OrganizationUnitId,
            item.ApprovalProfileId,
            item.EntityCode,
            item.ObjectId,
            item.CurrentLevel,
            item.ApprovalStateId,
            item.RequestedByUserId,
            item.UtcRequestedAt,
            item.UtcExpiresAt,
            item.Reason,
            item.IsActive)).ToList());
    }

    [HttpPost]
    [RequirePermission(Permissions.WorkflowApprovalInstanceCreate, true)]
    [ProducesResponseType(typeof(CreateApprovalInstanceResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CreateApprovalInstanceResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateApprovalInstanceResponseContract>> Create(
        [FromBody] CreateApprovalInstanceRequestContract request,
        CancellationToken cancellationToken)
    {
        CreateApprovalInstanceResponse result = await _createHandler.HandleAsync(
            new CreateApprovalInstanceRequest(
                request.OrganizationUnitId,
                request.ApprovalProfileId,
                request.EntityCode,
                request.ObjectId,
                request.CurrentLevel,
                request.ApprovalStateId,
                request.UtcExpiresAt,
                request.Reason,
                request.Payload,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        CreateApprovalInstanceResponseContract response = new(
            result.IsSuccess,
            result.ApprovalInstanceId,
            result.ErrorCode,
            result.ErrorMessage);

        if (!result.IsSuccess)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
