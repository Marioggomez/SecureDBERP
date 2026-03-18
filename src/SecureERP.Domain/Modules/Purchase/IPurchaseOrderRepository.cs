namespace SecureERP.Domain.Modules.Purchase;

public interface IPurchaseOrderRepository
{
    Task<long> CreateDraftAsync(
        PurchaseOrderToCreate request,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderSnapshot?> GetByIdAsync(
        long purchaseOrderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrderListItemSnapshot>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<bool> UpdateDraftAsync(
        PurchaseOrderToUpdate request,
        CancellationToken cancellationToken = default);

    Task<bool> UpsertDraftDetailAsync(
        PurchaseOrderDetailToUpsert detail,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderActionResultSnapshot> SubmitAsync(
        long purchaseOrderId,
        long userId,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderActionResultSnapshot> ApproveAsync(
        long purchaseOrderId,
        long userId,
        string? comment,
        CancellationToken cancellationToken = default);
}


