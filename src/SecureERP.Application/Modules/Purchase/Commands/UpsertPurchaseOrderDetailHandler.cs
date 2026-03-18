using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Commands;

public sealed class UpsertPurchaseOrderDetailHandler : IUpsertPurchaseOrderDetailHandler
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public UpsertPurchaseOrderDetailHandler(
        IPurchaseOrderRepository repository,
        IRequestContextAccessor requestContextAccessor)
    {
        _repository = repository;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<UpsertPurchaseOrderDetailResponse> HandleAsync(
        UpsertPurchaseOrderDetailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PurchaseOrderId <= 0)
        {
            return UpsertPurchaseOrderDetailResponse.Failure("PURCHASE_ORDER_ID_REQUIRED", "Purchase order id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return UpsertPurchaseOrderDetailResponse.Failure("PURCHASE_ORDER_DETAIL_DESCRIPTION_REQUIRED", "Detail description is required.");
        }

        if (request.Quantity <= 0 || request.EstimatedUnitCost < 0)
        {
            return UpsertPurchaseOrderDetailResponse.Failure("PURCHASE_ORDER_DETAIL_VALUES_INVALID", "Detail quantity and amount are invalid.");
        }

        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null)
        {
            return UpsertPurchaseOrderDetailResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required.");
        }

        bool updated = await _repository.UpsertDraftDetailAsync(
            new PurchaseOrderDetailToUpsert(
                request.PurchaseOrderId,
                request.PurchaseOrderDetailId,
                request.LineNumber,
                request.Description.Trim(),
                request.Quantity,
                request.EstimatedUnitCost,
                request.CostCenterCode,
                context.UserId.Value,
                DateTime.UtcNow),
            cancellationToken);

        return updated
            ? UpsertPurchaseOrderDetailResponse.Success()
            : UpsertPurchaseOrderDetailResponse.Failure("PURCHASE_ORDER_DETAIL_NOT_ALLOWED", "Only draft purchase orders can update details.");
    }
}


