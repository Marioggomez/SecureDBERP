using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface ICreatePurchaseRequestHandler
{
    Task<CreatePurchaseRequestResponse> HandleAsync(
        CreatePurchaseRequestRequest request,
        CancellationToken cancellationToken = default);
}
