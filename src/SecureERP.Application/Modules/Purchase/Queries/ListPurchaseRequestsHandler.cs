using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Queries;

public sealed class ListPurchaseRequestsHandler : IListPurchaseRequestsHandler
{
    private readonly IPurchaseRequestRepository _repository;

    public ListPurchaseRequestsHandler(IPurchaseRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PurchaseRequestListItemDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PurchaseRequestListItemSnapshot> rows = await _repository.ListAsync(cancellationToken);
        return rows.Select(row => new PurchaseRequestListItemDto(
            row.PurchaseRequestId,
            row.RequestNumber,
            row.RequestDate,
            (short)row.State,
            PurchaseRequestStateCodes.ToCode(row.State),
            row.CreatedByUserId,
            row.EstimatedTotal,
            row.IsActive,
            row.UtcCreatedAt,
            row.UtcUpdatedAt)).ToList();
    }
}
