using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Purchase.Abstractions;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Application.Modules.Security;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Purchase;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Purchase.Commands;

public sealed class ApprovePurchaseOrderHandler : IApprovePurchaseOrderHandler
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly IOperationalSecurityService _operationalSecurityService;
    private readonly IAuthRepository _authRepository;

    public ApprovePurchaseOrderHandler(
        IPurchaseOrderRepository repository,
        IRequestContextAccessor requestContextAccessor,
        IOperationalSecurityService operationalSecurityService,
        IAuthRepository authRepository)
    {
        _repository = repository;
        _requestContextAccessor = requestContextAccessor;
        _operationalSecurityService = operationalSecurityService;
        _authRepository = authRepository;
    }

    public async Task<ApprovePurchaseOrderResponse> HandleAsync(
        ApprovePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PurchaseOrderId <= 0)
        {
            return ApprovePurchaseOrderResponse.Failure("PURCHASE_ORDER_ID_REQUIRED", "Purchase order id is required.");
        }

        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null || context.SessionId is null)
        {
            return ApprovePurchaseOrderResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required.");
        }

        OperationalSecurityDecision guard = await _operationalSecurityService.GuardAsync(
            Permissions.PurchaseOrderApprove,
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
                    $"Purchase order approve blocked by operational policy ({guard.Code}).",
                    context.TenantId,
                    context.CompanyId,
                    context.UserId,
                    context.SessionId,
                    null,
                    ParseCorrelationId(context.CorrelationId),
                    request.IpAddress,
                    request.UserAgent),
                cancellationToken);

            return ApprovePurchaseOrderResponse.Failure("AUTH_REQUEST_REJECTED", "Operation rejected.");
        }

        PurchaseOrderActionResultSnapshot result = await _repository.ApproveAsync(
            request.PurchaseOrderId,
            context.UserId.Value,
            request.Comment,
            cancellationToken);

        if (!result.IsSuccess || result.NewState is null)
        {
            if (result.ErrorCode is "PURCHASE_ORDER_SOD_DENY" or "MFA_REQUIRED")
            {
                await _authRepository.WriteSecurityEventAsync(
                    new SecurityEventToCreate(
                        result.ErrorCode is "MFA_REQUIRED" ? "MFA_REQUIRED_DENY" : "SOD_DENY",
                        "WARNING",
                        "DENIED",
                        result.ErrorMessage ?? "Purchase order approval denied.",
                        context.TenantId,
                        context.CompanyId,
                        context.UserId,
                        context.SessionId,
                        null,
                        ParseCorrelationId(context.CorrelationId),
                        request.IpAddress,
                        request.UserAgent),
                    cancellationToken);
            }

            return ApprovePurchaseOrderResponse.Failure(
                result.ErrorCode ?? "PURCHASE_ORDER_APPROVE_FAILED",
                result.ErrorMessage ?? "Purchase order could not be approved.");
        }

        await _authRepository.WriteSecurityEventAsync(
            new SecurityEventToCreate(
                "PURCHASE_ORDER_APPROVED",
                "INFO",
                "OK",
                "Purchase order approved successfully.",
                context.TenantId,
                context.CompanyId,
                context.UserId,
                context.SessionId,
                null,
                ParseCorrelationId(context.CorrelationId),
                request.IpAddress,
                request.UserAgent),
            cancellationToken);

        return ApprovePurchaseOrderResponse.Success((short)result.NewState.Value, PurchaseOrderStateCodes.ToCode(result.NewState.Value));
    }

    private static Guid? ParseCorrelationId(string? correlationId)
        => Guid.TryParse(correlationId, out Guid value) ? value : null;
}


