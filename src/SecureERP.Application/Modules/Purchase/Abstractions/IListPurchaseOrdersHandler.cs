using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IListPurchaseOrdersHandler
{
    Task<IReadOnlyList<PurchaseOrderListItemDto>> HandleAsync(
        CancellationToken cancellationToken = default);
}


