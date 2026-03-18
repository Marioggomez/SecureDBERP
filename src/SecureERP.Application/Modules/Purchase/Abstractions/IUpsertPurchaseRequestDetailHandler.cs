using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IUpsertPurchaseRequestDetailHandler
{
    Task<UpsertPurchaseRequestDetailResponse> HandleAsync(
        UpsertPurchaseRequestDetailRequest request,
        CancellationToken cancellationToken = default);
}
