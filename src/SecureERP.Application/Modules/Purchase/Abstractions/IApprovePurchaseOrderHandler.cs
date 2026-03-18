using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IApprovePurchaseOrderHandler
{
    Task<ApprovePurchaseOrderResponse> HandleAsync(
        ApprovePurchaseOrderRequest request,
        CancellationToken cancellationToken = default);
}


