using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IUpdatePurchaseOrderDraftHandler
{
    Task<UpdatePurchaseOrderDraftResponse> HandleAsync(
        UpdatePurchaseOrderDraftRequest request,
        CancellationToken cancellationToken = default);
}


