using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase.Commands;

public sealed class CreatePurchaseRequestHandler : ICreatePurchaseRequestHandler
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public CreatePurchaseRequestHandler(
        IPurchaseRequestRepository repository,
        IRequestContextAccessor requestContextAccessor)
    {
        _repository = repository;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<CreatePurchaseRequestResponse> HandleAsync(
        CreatePurchaseRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null)
        {
            return CreatePurchaseRequestResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required.");
        }

        long id = await _repository.CreateDraftAsync(
            new PurchaseRequestToCreate(
                request.OrganizationUnitId,
                request.RequestDate == default ? DateTime.UtcNow.Date : request.RequestDate.Date,
                request.Notes,
                context.UserId.Value,
                DateTime.UtcNow),
            cancellationToken);

        if (id <= 0)
        {
            return CreatePurchaseRequestResponse.Failure("PURCHASE_REQUEST_CREATE_FAILED", "Purchase request could not be created.");
        }

        return CreatePurchaseRequestResponse.Success(id);
    }
}
