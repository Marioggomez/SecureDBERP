using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IUpdatePurchaseRequestDraftHandler
{
    Task<UpdatePurchaseRequestDraftResponse> HandleAsync(
        UpdatePurchaseRequestDraftRequest request,
        CancellationToken cancellationToken = default);
}
