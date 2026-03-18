using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IListPurchaseRequestsHandler
{
    Task<IReadOnlyList<PurchaseRequestListItemDto>> HandleAsync(
        CancellationToken cancellationToken = default);
}
