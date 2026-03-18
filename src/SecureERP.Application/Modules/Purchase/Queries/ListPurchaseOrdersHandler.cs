using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Queries;

public sealed class ListPurchaseOrdersHandler : IListPurchaseOrdersHandler
{
    private readonly IPurchaseOrderRepository _repository;

    public ListPurchaseOrdersHandler(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PurchaseOrderListItemDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PurchaseOrderListItemSnapshot> rows = await _repository.ListAsync(cancellationToken);
        return rows.Select(row => new PurchaseOrderListItemDto(
            row.PurchaseOrderId,
            row.RequestNumber,
            row.RequestDate,
            (short)row.State,
            PurchaseOrderStateCodes.ToCode(row.State),
            row.CreatedByUserId,
            row.EstimatedTotal,
            row.IsActive,
            row.UtcCreatedAt,
            row.UtcUpdatedAt)).ToList();
    }
}


