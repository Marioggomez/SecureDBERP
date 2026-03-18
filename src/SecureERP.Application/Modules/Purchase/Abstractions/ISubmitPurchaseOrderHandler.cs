using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface ISubmitPurchaseOrderHandler
{
    Task<SubmitPurchaseOrderResponse> HandleAsync(
        SubmitPurchaseOrderRequest request,
        CancellationToken cancellationToken = default);
}


