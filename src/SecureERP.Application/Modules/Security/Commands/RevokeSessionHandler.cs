using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Commands;

public sealed class RevokeSessionHandler : IRevokeSessionHandler
{
    private readonly ISecurityAdministrationRepository _securityAdministrationRepository;
    private readonly IAuthRepository _authRepository;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public RevokeSessionHandler(
        ISecurityAdministrationRepository securityAdministrationRepository,
        IAuthRepository authRepository,
        IRequestContextAccessor requestContextAccessor)
    {
        _securityAdministrationRepository = securityAdministrationRepository;
        _authRepository = authRepository;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<RevokeSessionResponse> HandleAsync(
        RevokeSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null || context.SessionId is null)
        {
            return new RevokeSessionResponse(false, "SESSION_CONTEXT_REQUIRED", "Security context is required.");
        }

        SessionRevocationResult result = await _securityAdministrationRepository.RevokeSessionAsync(
            request.SessionId,
            context.UserId.Value,
            request.Reason,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return new RevokeSessionResponse(false, result.ErrorCode, result.ErrorMessage);
        }

        await _authRepository.WriteSecurityEventAsync(
            new SecurityEventToCreate(
                "AUTH_SESSION_REVOKED",
                "WARNING",
                "OK",
                $"Session revoked. TargetSession={request.SessionId}; Reason={request.Reason ?? "N/A"}",
                context.TenantId,
                context.CompanyId,
                context.UserId,
                context.SessionId,
                null,
                ParseCorrelationId(context.CorrelationId),
                request.IpAddress,
                request.UserAgent),
            cancellationToken);

        return new RevokeSessionResponse(true, null, null);
    }

    private static Guid? ParseCorrelationId(string? correlationId)
    {
        return Guid.TryParse(correlationId, out Guid value) ? value : null;
    }
}
