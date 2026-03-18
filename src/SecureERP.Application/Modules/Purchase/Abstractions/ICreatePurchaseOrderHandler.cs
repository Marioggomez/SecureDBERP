using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface ICreatePurchaseOrderHandler
{
    Task<CreatePurchaseOrderResponse> HandleAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default);
}


