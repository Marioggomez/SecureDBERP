using Microsoft.AspNetCore.Mvc;
using SecureERP.Api.Modules.Security;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Application.Modules.Security;

namespace SecureERP.Api.Modules.Purchase;

[ApiController]
[Route("api/v1/purchase/orders")]
public sealed class PurchaseOrdersController : ControllerBase
{
    private readonly ICreatePurchaseOrderHandler _createHandler;
    private readonly IGetPurchaseOrderByIdHandler _getByIdHandler;
    private readonly IListPurchaseOrdersHandler _listHandler;
    private readonly IUpdatePurchaseOrderDraftHandler _updateDraftHandler;
    private readonly IUpsertPurchaseOrderDetailHandler _upsertDetailHandler;
    private readonly ISubmitPurchaseOrderHandler _submitHandler;
    private readonly IApprovePurchaseOrderHandler _approveHandler;

    public PurchaseOrdersController(
        ICreatePurchaseOrderHandler createHandler,
        IGetPurchaseOrderByIdHandler getByIdHandler,
        IListPurchaseOrdersHandler listHandler,
        IUpdatePurchaseOrderDraftHandler updateDraftHandler,
        IUpsertPurchaseOrderDetailHandler upsertDetailHandler,
        ISubmitPurchaseOrderHandler submitHandler,
        IApprovePurchaseOrderHandler approveHandler)
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
    [RequirePermission(Permissions.PurchaseOrderCreate)]
    [ProducesResponseType(typeof(CreatePurchaseOrderResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CreatePurchaseOrderResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePurchaseOrderResponseContract>> Create(
        [FromBody] CreatePurchaseOrderRequestContract request,
        CancellationToken cancellationToken)
    {
        CreatePurchaseOrderResponse result = await _createHandler.HandleAsync(
            new CreatePurchaseOrderRequest(
                request.OrganizationUnitId,
                request.RequestDate,
                request.Notes,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        CreatePurchaseOrderResponseContract response = new(
            result.IsSuccess,
            result.PurchaseOrderId,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.PurchaseOrderRead)]
    [ProducesResponseType(typeof(PurchaseOrderContract), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderContract>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        PurchaseOrderDto? result = await _getByIdHandler.HandleAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(Map(result));
    }

    [HttpGet]
    [RequirePermission(Permissions.PurchaseOrderRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseOrderListItemContract>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderListItemContract>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<PurchaseOrderListItemDto> result = await _listHandler.HandleAsync(cancellationToken);
        return Ok(result.Select(item => new PurchaseOrderListItemContract(
            item.PurchaseOrderId,
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
    [RequirePermission(Permissions.PurchaseOrderUpdate)]
    [ProducesResponseType(typeof(UpdatePurchaseOrderDraftResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UpdatePurchaseOrderDraftResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdatePurchaseOrderDraftResponseContract>> UpdateDraft(
        long id,
        [FromBody] UpdatePurchaseOrderDraftRequestContract request,
        CancellationToken cancellationToken)
    {
        UpdatePurchaseOrderDraftResponse result = await _updateDraftHandler.HandleAsync(
            new UpdatePurchaseOrderDraftRequest(
                id,
                request.OrganizationUnitId,
                request.RequestDate,
                request.Notes,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        UpdatePurchaseOrderDraftResponseContract response = new(
            result.IsSuccess,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id:long}/details")]
    [RequirePermission(Permissions.PurchaseOrderUpdate)]
    [ProducesResponseType(typeof(UpsertPurchaseOrderDetailResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UpsertPurchaseOrderDetailResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpsertPurchaseOrderDetailResponseContract>> UpsertDetail(
        long id,
        [FromBody] UpsertPurchaseOrderDetailRequestContract request,
        CancellationToken cancellationToken)
    {
        UpsertPurchaseOrderDetailResponse result = await _upsertDetailHandler.HandleAsync(
            new UpsertPurchaseOrderDetailRequest(
                id,
                request.PurchaseOrderDetailId,
                request.LineNumber,
                request.Description,
                request.Quantity,
                request.EstimatedUnitCost,
                request.CostCenterCode,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        UpsertPurchaseOrderDetailResponseContract response = new(
            result.IsSuccess,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:long}/submit")]
    [RequirePermission(Permissions.PurchaseOrderSubmit)]
    [ProducesResponseType(typeof(SubmitPurchaseOrderResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SubmitPurchaseOrderResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitPurchaseOrderResponseContract>> Submit(
        long id,
        CancellationToken cancellationToken)
    {
        SubmitPurchaseOrderResponse result = await _submitHandler.HandleAsync(
            new SubmitPurchaseOrderRequest(
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        SubmitPurchaseOrderResponseContract response = new(
            result.IsSuccess,
            result.NewStateId,
            result.NewStateCode,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:long}/approve")]
    [RequirePermission(Permissions.PurchaseOrderApprove, true)]
    [ProducesResponseType(typeof(ApprovePurchaseOrderResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApprovePurchaseOrderResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApprovePurchaseOrderResponseContract>> Approve(
        long id,
        [FromBody] ApprovePurchaseOrderRequestContract request,
        CancellationToken cancellationToken)
    {
        ApprovePurchaseOrderResponse result = await _approveHandler.HandleAsync(
            new ApprovePurchaseOrderRequest(
                id,
                request.Comment,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        ApprovePurchaseOrderResponseContract response = new(
            result.IsSuccess,
            result.NewStateId,
            result.NewStateCode,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }

    private static PurchaseOrderContract Map(PurchaseOrderDto dto)
    {
        return new PurchaseOrderContract(
            dto.PurchaseOrderId,
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
            dto.Details.Select(detail => new PurchaseOrderDetailContract(
                detail.PurchaseOrderDetailId,
                detail.PurchaseOrderId,
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


