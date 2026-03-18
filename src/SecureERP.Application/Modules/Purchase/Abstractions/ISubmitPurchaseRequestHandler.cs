using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface ISubmitPurchaseRequestHandler
{
    Task<SubmitPurchaseRequestResponse> HandleAsync(
        SubmitPurchaseRequestRequest request,
        CancellationToken cancellationToken = default);
}
