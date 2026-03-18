using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Commands;

public sealed class CreatePurchaseOrderHandler : ICreatePurchaseOrderHandler
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public CreatePurchaseOrderHandler(
        IPurchaseOrderRepository repository,
        IRequestContextAccessor requestContextAccessor)
    {
        _repository = repository;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<CreatePurchaseOrderResponse> HandleAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null)
        {
            return CreatePurchaseOrderResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required.");
        }

        long id = await _repository.CreateDraftAsync(
            new PurchaseOrderToCreate(
                request.OrganizationUnitId,
                request.RequestDate == default ? DateTime.UtcNow.Date : request.RequestDate.Date,
                request.Notes,
                context.UserId.Value,
                DateTime.UtcNow),
            cancellationToken);

        if (id <= 0)
        {
            return CreatePurchaseOrderResponse.Failure("PURCHASE_ORDER_CREATE_FAILED", "Purchase order could not be created.");
        }

        return CreatePurchaseOrderResponse.Success(id);
    }
}


