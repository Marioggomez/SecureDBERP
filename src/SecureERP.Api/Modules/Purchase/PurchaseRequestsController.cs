using Microsoft.AspNetCore.Mvc;
using SecureERP.Api.Modules.Security;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Application.Modules.Security;

namespace SecureERP.Api.Modules.Purchase;

[ApiController]
[Route("api/v1/purchase/requests")]
public sealed class PurchaseRequestsController : ControllerBase
{
    private readonly ICreatePurchaseRequestHandler _createHandler;
    private readonly IGetPurchaseRequestByIdHandler _getByIdHandler;
    private readonly IListPurchaseRequestsHandler _listHandler;
    private readonly IUpdatePurchaseRequestDraftHandler _updateDraftHandler;
    private readonly IUpsertPurchaseRequestDetailHandler _upsertDetailHandler;
    private readonly ISubmitPurchaseRequestHandler _submitHandler;
    private readonly IApprovePurchaseRequestHandler _approveHandler;

    public PurchaseRequestsController(
        ICreatePurchaseRequestHandler createHandler,
        IGetPurchaseRequestByIdHandler getByIdHandler,
        IListPurchaseRequestsHandler listHandler,
        IUpdatePurchaseRequestDraftHandler updateDraftHandler,
        IUpsertPurchaseRequestDetailHandler upsertDetailHandler,
        ISubmitPurchaseRequestHandler submitHandler,
        IApprovePurchaseRequestHandler approveHandler)
    {
        _createHandler = createHandler;
        _getByIdHandler = getByIdHandler;
        _listHandler = listHandler;
        _updateDraftHandler = updateDraftHandler;
        _upsertDetailHandler = upsertDetailHandler;
        _submitHandler = submitHandler;
        _approveHandler = approveHandler;
    }

    [HttpPost]
    [RequirePermission(Permissions.PurchaseRequestCreate)]
    [ProducesResponseType(typeof(CreatePurchaseRequestResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CreatePurchaseRequestResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePurchaseRequestResponseContract>> Create(
        [FromBody] CreatePurchaseRequestRequestContract request,
        CancellationToken cancellationToken)
    {
        CreatePurchaseRequestResponse result = await _createHandler.HandleAsync(
            new CreatePurchaseRequestRequest(
                request.OrganizationUnitId,
                request.RequestDate,
                request.Notes,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        CreatePurchaseRequestResponseContract response = new(
            result.IsSuccess,
            result.PurchaseRequestId,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.PurchaseRequestRead)]
    [ProducesResponseType(typeof(PurchaseRequestContract), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseRequestContract>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        PurchaseRequestDto? result = await _getByIdHandler.HandleAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(Map(result));
    }

    [HttpGet]
    [RequirePermission(Permissions.PurchaseRequestRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseRequestListItemContract>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PurchaseRequestListItemContract>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<PurchaseRequestListItemDto> result = await _listHandler.HandleAsync(cancellationToken);
        return Ok(result.Select(item => new PurchaseRequestListItemContract(
            item.PurchaseRequestId,
            item.RequestNumber,
            item.RequestDate,
            item.StateId,
            item.StateCode,
            item.CreatedByUserId,
            item.EstimatedTotal,
            item.IsActive,
            item.UtcCreatedAt,
            item.UtcUpdatedAt)).ToList());
    }

    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.PurchaseRequestUpdate)]
    [ProducesResponseType(typeof(UpdatePurchaseRequestDraftResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UpdatePurchaseRequestDraftResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdatePurchaseRequestDraftResponseContract>> UpdateDraft(
        long id,
        [FromBody] UpdatePurchaseRequestDraftRequestContract request,
        CancellationToken cancellationToken)
    {
        UpdatePurchaseRequestDraftResponse result = await _updateDraftHandler.HandleAsync(
            new UpdatePurchaseRequestDraftRequest(
                id,
                request.OrganizationUnitId,
                request.RequestDate,
                request.Notes,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        UpdatePurchaseRequestDraftResponseContract response = new(
            result.IsSuccess,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id:long}/details")]
    [RequirePermission(Permissions.PurchaseRequestUpdate)]
    [ProducesResponseType(typeof(UpsertPurchaseRequestDetailResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UpsertPurchaseRequestDetailResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpsertPurchaseRequestDetailResponseContract>> UpsertDetail(
        long id,
        [FromBody] UpsertPurchaseRequestDetailRequestContract request,
        CancellationToken cancellationToken)
    {
        UpsertPurchaseRequestDetailResponse result = await _upsertDetailHandler.HandleAsync(
            new UpsertPurchaseRequestDetailRequest(
                id,
                request.PurchaseRequestDetailId,
                request.LineNumber,
                request.Description,
                request.Quantity,
                request.EstimatedUnitCost,
                request.CostCenterCode,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        UpsertPurchaseRequestDetailResponseContract response = new(
            result.IsSuccess,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:long}/submit")]
    [RequirePermission(Permissions.PurchaseRequestSubmit)]
    [ProducesResponseType(typeof(SubmitPurchaseRequestResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SubmitPurchaseRequestResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitPurchaseRequestResponseContract>> Submit(
        long id,
        CancellationToken cancellationToken)
    {
        SubmitPurchaseRequestResponse result = await _submitHandler.HandleAsync(
            new SubmitPurchaseRequestRequest(
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        SubmitPurchaseRequestResponseContract response = new(
            result.IsSuccess,
            result.NewStateId,
            result.NewStateCode,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:long}/approve")]
    [RequirePermission(Permissions.PurchaseRequestApprove, true)]
    [ProducesResponseType(typeof(ApprovePurchaseRequestResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApprovePurchaseRequestResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApprovePurchaseRequestResponseContract>> Approve(
        long id,
        [FromBody] ApprovePurchaseRequestRequestContract request,
        CancellationToken cancellationToken)
    {
        ApprovePurchaseRequestResponse result = await _approveHandler.HandleAsync(
            new ApprovePurchaseRequestRequest(
                id,
                request.Comment,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        ApprovePurchaseRequestResponseContract response = new(
            result.IsSuccess,
            result.NewStateId,
            result.NewStateCode,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    private static PurchaseRequestContract Map(PurchaseRequestDto dto)
    {
        return new PurchaseRequestContract(
            dto.PurchaseRequestId,
            dto.TenantId,
            dto.CompanyId,
            dto.OrganizationUnitId,
            dto.RequestNumber,
            dto.RequestDate,
            dto.StateId,
            dto.StateCode,
            dto.CreatedByUserId,
            dto.UtcCreatedAt,
            dto.UpdatedByUserId,
            dto.UtcUpdatedAt,
            dto.Notes,
            dto.EstimatedTotal,
            dto.IsActive,
            dto.Details.Select(detail => new PurchaseRequestDetailContract(
                detail.PurchaseRequestDetailId,
                detail.PurchaseRequestId,
                detail.LineNumber,
                detail.Description,
                detail.Quantity,
                detail.EstimatedUnitCost,
                detail.EstimatedTotal,
                detail.CostCenterCode,
                detail.IsActive,
                detail.UtcCreatedAt,
                detail.UtcUpdatedAt)).ToList());
    }
}
