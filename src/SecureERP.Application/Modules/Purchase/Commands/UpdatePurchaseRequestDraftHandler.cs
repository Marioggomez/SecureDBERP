using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Commands;

public sealed class UpdatePurchaseRequestDraftHandler : IUpdatePurchaseRequestDraftHandler
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public UpdatePurchaseRequestDraftHandler(
        IPurchaseRequestRepository repository,
        IRequestContextAccessor requestContextAccessor)
    {
        _repository = repository;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<UpdatePurchaseRequestDraftResponse> HandleAsync(
        UpdatePurchaseRequestDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PurchaseRequestId <= 0)
        {
            return UpdatePurchaseRequestDraftResponse.Failure("PURCHASE_REQUEST_ID_REQUIRED", "Purchase request id is required.");
        }

        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null)
        {
            return UpdatePurchaseRequestDraftResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required.");
        }

        bool updated = await _repository.UpdateDraftAsync(
            new PurchaseRequestToUpdate(
                request.PurchaseRequestId,
                request.OrganizationUnitId,
                request.RequestDate == default ? DateTime.UtcNow.Date : request.RequestDate.Date,
                request.Notes,
                context.UserId.Value,
                DateTime.UtcNow),
            cancellationToken);

        return updated
            ? UpdatePurchaseRequestDraftResponse.Success()
            : UpdatePurchaseRequestDraftResponse.Failure("PURCHASE_REQUEST_UPDATE_NOT_ALLOWED", "Only draft purchase requests can be updated.");
    }
}
