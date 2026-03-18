using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Commands;

public sealed class UpsertPurchaseRequestDetailHandler : IUpsertPurchaseRequestDetailHandler
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public UpsertPurchaseRequestDetailHandler(
        IPurchaseRequestRepository repository,
        IRequestContextAccessor requestContextAccessor)
    {
        _repository = repository;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<UpsertPurchaseRequestDetailResponse> HandleAsync(
        UpsertPurchaseRequestDetailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PurchaseRequestId <= 0)
        {
            return UpsertPurchaseRequestDetailResponse.Failure("PURCHASE_REQUEST_ID_REQUIRED", "Purchase request id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return UpsertPurchaseRequestDetailResponse.Failure("PURCHASE_REQUEST_DETAIL_DESCRIPTION_REQUIRED", "Detail description is required.");
        }

        if (request.Quantity <= 0 || request.EstimatedUnitCost < 0)
        {
            return UpsertPurchaseRequestDetailResponse.Failure("PURCHASE_REQUEST_DETAIL_VALUES_INVALID", "Detail quantity and amount are invalid.");
        }

        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null)
        {
            return UpsertPurchaseRequestDetailResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required.");
        }

        bool updated = await _repository.UpsertDraftDetailAsync(
            new PurchaseRequestDetailToUpsert(
                request.PurchaseRequestId,
                request.PurchaseRequestDetailId,
                request.LineNumber,
                request.Description.Trim(),
                request.Quantity,
                request.EstimatedUnitCost,
                request.CostCenterCode,
                context.UserId.Value,
                DateTime.UtcNow),
            cancellationToken);

        return updated
            ? UpsertPurchaseRequestDetailResponse.Success()
            : UpsertPurchaseRequestDetailResponse.Failure("PURCHASE_REQUEST_DETAIL_NOT_ALLOWED", "Only draft purchase requests can update details.");
    }
}
