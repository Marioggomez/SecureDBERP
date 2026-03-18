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
        string otpCode = _mfaCodeService.GenerateCode();
        byte[] salt = _mfaCodeService.GenerateSalt(16);
        byte[] hash = _mfaCodeService.ComputeHash(otpCode, salt);
        Guid challengeId = Guid.NewGuid();
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        long userId;
        long tenantId;
        long? companyId;
        Guid? sessionId;
        Guid? authFlowId;

        if (request.Purpose == MfaPurpose.Login)
        {
            if (request.AuthFlowId is null)
            {
                return RequestMfaChallengeResponse.Failure("AUTH_FLOW_REQUIRED", "AuthFlowId is required for login MFA.");
            }

            AuthFlowSnapshot? flow = await _authRepository.GetAuthFlowAsync(request.AuthFlowId.Value, cancellationToken);
            if (flow is null || flow.IsUsed || flow.UtcExpiresAt < DateTime.UtcNow)
            {
                return RequestMfaChallengeResponse.Failure("AUTH_FLOW_INVALID", "Authentication flow is invalid.");
            }

            userId = flow.UserId;
            tenantId = flow.TenantId;
            IReadOnlyList<OperableCompany> companies = await _authRepository.GetOperableCompaniesAsync(
                flow.UserId,
                flow.TenantId,
                cancellationToken);
            OperableCompany? defaultCompany = companies.FirstOrDefault(c => c.IsDefault) ?? companies.FirstOrDefault();
            if (defaultCompany is null)
            {
                return RequestMfaChallengeResponse.Failure("AUTH_COMPANY_NOT_FOUND", "No operable company available for login MFA.");
            }

            companyId = defaultCompany.CompanyId;
            sessionId = null;
            authFlowId = flow.AuthFlowId;

            _requestContextAccessor.SetCurrent(new RequestContext(
                tenantId,
                companyId,
                userId,
                null,
                context.CorrelationId));
        }
        else if (request.Purpose == MfaPurpose.StepUp)
        {
            if (context.UserId is null || context.TenantId is null || context.CompanyId is null || context.SessionId is null)
            {
                return RequestMfaChallengeResponse.Failure("SESSION_CONTEXT_REQUIRED", "Valid session context is required for step-up MFA.");
            }

            userId = context.UserId.Value;
            tenantId = context.TenantId.Value;
            companyId = context.CompanyId.Value;
            sessionId = context.SessionId.Value;
            authFlowId = null;
        }
        else
        {
            return RequestMfaChallengeResponse.Failure("MFA_PURPOSE_INVALID", "Unsupported MFA purpose.");
        }

        await _authRepository.CreateMfaChallengeAsync(
            new MfaChallengeToCreate(
                challengeId,
                userId,
                tenantId,
                companyId,
                sessionId,
                authFlowId,
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
                request.Purpose == MfaPurpose.Login
                    ? $"Login MFA challenge created via {request.Channel}."
                    : $"Step-up MFA challenge created via {request.Channel}.",
                tenantId,
                companyId,
                userId,
                sessionId,
                authFlowId,
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
