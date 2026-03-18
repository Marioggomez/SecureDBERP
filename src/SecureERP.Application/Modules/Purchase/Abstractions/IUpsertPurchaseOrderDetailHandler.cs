using SecureERP.Application.Modules.Purchase.DTOs;

namespace SecureERP.Application.Modules.Purchase.Abstractions;

public interface IUpsertPurchaseOrderDetailHandler
{
    Task<UpsertPurchaseOrderDetailResponse> HandleAsync(
        UpsertPurchaseOrderDetailRequest request,
        CancellationToken cancellationToken = default);
}


