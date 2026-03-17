using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Commands;

public sealed class RequestMfaChallengeHandler : IRequestMfaChallengeHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly IMfaCodeService _mfaCodeService;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public RequestMfaChallengeHandler(
        IAuthRepository authRepository,
        IMfaCodeService mfaCodeService,
        IRequestContextAccessor requestContextAccessor)
    {
        _authRepository = authRepository;
        _mfaCodeService = mfaCodeService;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<RequestMfaChallengeResponse> HandleAsync(RequestMfaChallengeRequest request, CancellationToken cancellationToken = default)
    {
        RequestContext context = _requestContextAccessor.Current;
        if (context.UserId is null || context.TenantId is null || context.CompanyId is null)
        {
            return RequestMfaChallengeResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required.");
        }

        AuthFlowSnapshot? flow = await _authRepository.GetAuthFlowAsync(request.AuthFlowId, cancellationToken);
        if (flow is null || flow.IsUsed || flow.UtcExpiresAt < DateTime.UtcNow)
        {
            return RequestMfaChallengeResponse.Failure("AUTH_FLOW_INVALID", "Authentication flow is invalid.");
        }

        string otpCode = _mfaCodeService.GenerateCode();
        byte[] salt = _mfaCodeService.GenerateSalt(16);
        byte[] hash = _mfaCodeService.ComputeHash(otpCode, salt);
        Guid challengeId = Guid.NewGuid();
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

        await _authRepository.CreateMfaChallengeAsync(
            new MfaChallengeToCreate(
                challengeId,
                context.UserId.Value,
                context.TenantId.Value,
                context.CompanyId.Value,
                request.AuthFlowId,
                request.Purpose,
                request.Channel,
                request.ActionCode,
                hash,
                salt,
                expiresAt.UtcDateTime,
                5),
            cancellationToken);

        await _authRepository.WriteSecurityEventAsync(
            new SecurityEventToCreate(
                "AUTH_MFA_CHALLENGE_CREATED",
                "INFO",
                "OK",
                $"MFA challenge created via {request.Channel}.",
                context.TenantId,
                context.CompanyId,
                context.UserId,
                context.SessionId,
                request.AuthFlowId,
                ParseCorrelationId(context.CorrelationId),
                null,
                null),
            cancellationToken);

        return RequestMfaChallengeResponse.Success(
            challengeId,
            expiresAt,
            "MFA code delivered via selected channel.");
    }

    private static Guid? ParseCorrelationId(string? correlationId)
        => Guid.TryParse(correlationId, out Guid value) ? value : null;
}
