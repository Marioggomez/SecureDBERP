using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IGetPurchaseRequestByIdHandler
{
    Task<PurchaseRequestDto?> HandleAsync(
        long purchaseRequestId,
        CancellationToken cancellationToken = default);
}
