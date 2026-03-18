using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Purchase;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Purchase.Commands;

public sealed class SubmitPurchaseRequestHandler : ISubmitPurchaseRequestHandler
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly IOperationalSecurityService _operationalSecurityService;
    private readonly IAuthRepository _authRepository;

    public SubmitPurchaseRequestHandler(
        IPurchaseRequestRepository repository,
        IRequestContextAccessor requestContextAccessor,
        IOperationalSecurityService operationalSecurityService,
        IAuthRepository authRepository)
    {
        _repository = repository;
        _requestContextAccessor = requestContextAccessor;
        _operationalSecurityService = operationalSecurityService;
        _authRepository = authRepository;
    }

    public async Task<SubmitPurchaseRequestResponse> HandleAsync(
        SubmitPurchaseRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PurchaseRequestId <= 0)
        {
            return SubmitPurchaseRequestResponse.Failure("PURCHASE_REQUEST_ID_REQUIRED", "Purchase request id is required.");
        }

        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null || context.SessionId is null)
        {
            return SubmitPurchaseRequestResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required.");
        }

        OperationalSecurityDecision guard = await _operationalSecurityService.GuardAsync(
            "PURCHASE.REQUEST.SUBMIT",
            request.IpAddress,
            $"USER:{context.UserId.Value}",
            context.TenantId,
            context.CompanyId,
            cancellationToken);

        if (!guard.IsAllowed)
        {
            await _authRepository.WriteSecurityEventAsync(
                new SecurityEventToCreate(
                    guard.Code.StartsWith("IP_", StringComparison.OrdinalIgnoreCase) ? "IP_POLICY_DENY" : "AUTH_ABUSE_DETECTED",
                    "WARNING",
                    "DENIED",
                    $"Purchase request submit blocked by operational policy ({guard.Code}).",
                    context.TenantId,
                    context.CompanyId,
                    context.UserId,
                    context.SessionId,
                    null,
                    ParseCorrelationId(context.CorrelationId),
                    request.IpAddress,
                    request.UserAgent),
                cancellationToken);

            return SubmitPurchaseRequestResponse.Failure("AUTH_REQUEST_REJECTED", "Operation rejected.");
        }

        PurchaseRequestActionResultSnapshot result = await _repository.SubmitAsync(
            request.PurchaseRequestId,
            context.UserId.Value,
            cancellationToken);

        if (!result.IsSuccess || result.NewState is null)
        {
            return SubmitPurchaseRequestResponse.Failure(
                result.ErrorCode ?? "PURCHASE_REQUEST_SUBMIT_FAILED",
                result.ErrorMessage ?? "Purchase request could not be submitted.");
        }

        return SubmitPurchaseRequestResponse.Success((short)result.NewState.Value, PurchaseRequestStateCodes.ToCode(result.NewState.Value));
    }

    private static Guid? ParseCorrelationId(string? correlationId)
        => Guid.TryParse(correlationId, out Guid value) ? value : null;
}
