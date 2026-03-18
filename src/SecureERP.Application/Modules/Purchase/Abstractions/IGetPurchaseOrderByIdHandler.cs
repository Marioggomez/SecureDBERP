using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IGetPurchaseOrderByIdHandler
{
    Task<PurchaseOrderDto?> HandleAsync(
        long purchaseOrderId,
        CancellationToken cancellationToken = default);
}


