using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Queries;

public sealed class GetPurchaseRequestByIdHandler : IGetPurchaseRequestByIdHandler
{
    private readonly IPurchaseRequestRepository _repository;

    public GetPurchaseRequestByIdHandler(IPurchaseRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<PurchaseRequestDto?> HandleAsync(long purchaseRequestId, CancellationToken cancellationToken = default)
    {
        PurchaseRequestSnapshot? request = await _repository.GetByIdAsync(purchaseRequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        return new PurchaseRequestDto(
            request.PurchaseRequestId,
            request.TenantId,
            request.CompanyId,
            request.OrganizationUnitId,
            request.RequestNumber,
            request.RequestDate,
            (short)request.State,
            PurchaseRequestStateCodes.ToCode(request.State),
            request.CreatedByUserId,
            request.UtcCreatedAt,
            request.UpdatedByUserId,
            request.UtcUpdatedAt,
            request.Notes,
            request.EstimatedTotal,
            request.IsActive,
            request.Details.Select(detail => new PurchaseRequestDetailDto(
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
