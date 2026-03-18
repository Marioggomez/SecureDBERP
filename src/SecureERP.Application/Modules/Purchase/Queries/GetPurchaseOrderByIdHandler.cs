using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Queries;

public sealed class GetPurchaseOrderByIdHandler : IGetPurchaseOrderByIdHandler
{
    private readonly IPurchaseOrderRepository _repository;

    public GetPurchaseOrderByIdHandler(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<PurchaseOrderDto?> HandleAsync(long purchaseOrderId, CancellationToken cancellationToken = default)
    {
        PurchaseOrderSnapshot? request = await _repository.GetByIdAsync(purchaseOrderId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        return new PurchaseOrderDto(
            request.PurchaseOrderId,
            request.TenantId,
            request.CompanyId,
            request.OrganizationUnitId,
            request.RequestNumber,
            request.RequestDate,
            (short)request.State,
            PurchaseOrderStateCodes.ToCode(request.State),
            request.CreatedByUserId,
            request.UtcCreatedAt,
            request.UpdatedByUserId,
            request.UtcUpdatedAt,
            request.Notes,
            request.EstimatedTotal,
            request.IsActive,
            request.Details.Select(detail => new PurchaseOrderDetailDto(
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


