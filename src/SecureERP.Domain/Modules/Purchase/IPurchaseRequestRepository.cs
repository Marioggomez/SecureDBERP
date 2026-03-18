namespace SecureERP.Domain.Modules.Purchase;

public interface IPurchaseRequestRepository
{
    Task<long> CreateDraftAsync(
        PurchaseRequestToCreate request,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequestSnapshot?> GetByIdAsync(
        long purchaseRequestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseRequestListItemSnapshot>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<bool> UpdateDraftAsync(
        PurchaseRequestToUpdate request,
        CancellationToken cancellationToken = default);

    Task<bool> UpsertDraftDetailAsync(
        PurchaseRequestDetailToUpsert detail,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequestActionResultSnapshot> SubmitAsync(
        long purchaseRequestId,
        long userId,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequestActionResultSnapshot> ApproveAsync(
        long purchaseRequestId,
        long userId,
        string? comment,
        CancellationToken cancellationToken = default);
}
