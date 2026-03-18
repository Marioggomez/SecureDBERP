using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IApprovePurchaseRequestHandler
{
    Task<ApprovePurchaseRequestResponse> HandleAsync(
        ApprovePurchaseRequestRequest request,
        CancellationToken cancellationToken = default);
}
