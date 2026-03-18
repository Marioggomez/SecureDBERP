using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Commands;

public sealed class UpdatePurchaseOrderDraftHandler : IUpdatePurchaseOrderDraftHandler
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public UpdatePurchaseOrderDraftHandler(
        IPurchaseOrderRepository repository,
        IRequestContextAccessor requestContextAccessor)
    {
        _repository = repository;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<UpdatePurchaseOrderDraftResponse> HandleAsync(
        UpdatePurchaseOrderDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PurchaseOrderId <= 0)
        {
            return UpdatePurchaseOrderDraftResponse.Failure("PURCHASE_ORDER_ID_REQUIRED", "Purchase order id is required.");
        }

        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null)
        {
            return UpdatePurchaseOrderDraftResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required.");
        }

        bool updated = await _repository.UpdateDraftAsync(
            new PurchaseOrderToUpdate(
                request.PurchaseOrderId,
                request.OrganizationUnitId,
                request.RequestDate == default ? DateTime.UtcNow.Date : request.RequestDate.Date,
                request.Notes,
                context.UserId.Value,
                DateTime.UtcNow),
            cancellationToken);

        return updated
            ? UpdatePurchaseOrderDraftResponse.Success()
            : UpdatePurchaseOrderDraftResponse.Failure("PURCHASE_ORDER_UPDATE_NOT_ALLOWED", "Only draft purchase orders can be updated.");
    }
}


