using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;
using System.Security.Cryptography;

namespace SecureERP.Application.Modules.Security.Commands;

public sealed class VerifyMfaChallengeHandler : IVerifyMfaChallengeHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly IMfaCodeService _mfaCodeService;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly IOperationalSecurityService _operationalSecurityService;

    public VerifyMfaChallengeHandler(
        IAuthRepository authRepository,
        IMfaCodeService mfaCodeService,
        IRequestContextAccessor requestContextAccessor,
        IOperationalSecurityService operationalSecurityService)
    {
        _authRepository = authRepository;
        _mfaCodeService = mfaCodeService;
        _requestContextAccessor = requestContextAccessor;
        _operationalSecurityService = operationalSecurityService;
    }

    public async Task<VerifyMfaChallengeResponse> HandleAsync(VerifyMfaChallengeRequest request, CancellationToken cancellationToken = default)
    {
        RequestContext current = _requestContextAccessor.Current;
        string safeIp = string.IsNullOrWhiteSpace(request.IpAddress) ? "0.0.0.0" : request.IpAddress.Trim();
        OperationalSecurityDecision guard = await _operationalSecurityService.GuardAsync(
            "AUTH.MFA.VERIFY",
            safeIp,
            request.ChallengeId.ToString(),
            current.TenantId,
            current.CompanyId,
            cancellationToken);
        if (!guard.IsAllowed)
        {
            string eventType = guard.Code.StartsWith("IP_", StringComparison.OrdinalIgnoreCase)
                ? "IP_POLICY_DENY"
                : "MFA_RATE_LIMIT_HIT";
            await _authRepository.WriteSecurityEventAsync(
                new SecurityEventToCreate(
                    eventType,
                    "WARNING",
                    "DENIED",
                    $"MFA verify blocked by operational policy ({guard.Code}).",
                    current.TenantId,
                    current.CompanyId,
                    current.UserId,
                    current.SessionId,
                    null,
                    ParseCorrelationId(current.CorrelationId),
                    safeIp,
                    request.UserAgent),
                cancellationToken);
            return VerifyMfaChallengeResponse.Failure("AUTH_REQUEST_REJECTED", "Operation rejected.");
        }

        if (string.IsNullOrWhiteSpace(request.OtpCode))
        {
            return VerifyMfaChallengeResponse.Failure("MFA_CODE_REQUIRED", "OTP code is required.");
        }

        MfaChallengeSnapshot? challenge = await _authRepository.GetMfaChallengeAsync(request.ChallengeId, cancellationToken);
        if (challenge is null)
        {
            return VerifyMfaChallengeResponse.Failure("MFA_CHALLENGE_NOT_FOUND", "MFA challenge was not found.");
        }

        if (challenge.IsUsed)
        {
            return VerifyMfaChallengeResponse.Failure("MFA_CHALLENGE_USED", "MFA challenge was already used.");
        }

        if (challenge.UtcExpiresAt < DateTime.UtcNow)
        {
            return VerifyMfaChallengeResponse.Failure("MFA_CHALLENGE_EXPIRED", "MFA challenge expired.");
        }

        if (challenge.Attempts >= challenge.MaxAttempts)
        {
            return VerifyMfaChallengeResponse.Failure("MFA_ATTEMPTS_EXCEEDED", "MFA attempts exceeded.");
        }

        byte[] computedHash = _mfaCodeService.ComputeHash(request.OtpCode.Trim(), challenge.OtpSalt);
        bool valid = CryptographicOperations.FixedTimeEquals(computedHash, challenge.OtpHash);
        if (!valid)
        {
            await _authRepository.IncrementMfaChallengeAttemptAsync(challenge.ChallengeId, cancellationToken);
            await _authRepository.WriteSecurityEventAsync(
                new SecurityEventToCreate(
                    "AUTH_MFA_CHALLENGE_FAILED",
                    "WARNING",
                    "DENIED",
                    "Invalid MFA challenge code.",
                    challenge.TenantId,
                    challenge.CompanyId,
                    challenge.UserId,
                    challenge.SessionId,
                    challenge.AuthFlowId,
                    ParseCorrelationId(_requestContextAccessor.Current.CorrelationId),
                    safeIp,
                    request.UserAgent),
                cancellationToken);
            return VerifyMfaChallengeResponse.Failure("MFA_CODE_INVALID", "Invalid MFA code.");
        }

        await _authRepository.MarkMfaChallengeValidatedAsync(challenge.ChallengeId, cancellationToken);
        if (challenge.Purpose == MfaPurpose.Login)
        {
            if (challenge.AuthFlowId is null)
            {
                return VerifyMfaChallengeResponse.Failure("MFA_AUTH_FLOW_REQUIRED", "Login MFA challenge requires AuthFlow.");
            }

            await _authRepository.MarkAuthFlowMfaValidatedAsync(challenge.AuthFlowId.Value, cancellationToken);
        }
        else if (challenge.Purpose == MfaPurpose.StepUp)
        {
            RequestContext context = _requestContextAccessor.Current;
            if (context.SessionId is null || challenge.SessionId is null || context.SessionId.Value != challenge.SessionId.Value)
            {
                return VerifyMfaChallengeResponse.Failure("MFA_SESSION_CONTEXT_REQUIRED", "Valid session context is required for step-up MFA verification.");
            }

            await _authRepository.MarkSessionMfaValidatedAsync(challenge.SessionId.Value, cancellationToken);
        }
        else
        {
            return VerifyMfaChallengeResponse.Failure("MFA_PURPOSE_INVALID", "Unsupported MFA purpose.");
        }

        await _authRepository.WriteSecurityEventAsync(
            new SecurityEventToCreate(
                "AUTH_MFA_CHALLENGE_VERIFIED",
                "INFO",
                "OK",
                "MFA challenge verified.",
                challenge.TenantId,
                challenge.CompanyId,
                challenge.UserId,
                challenge.SessionId,
                challenge.AuthFlowId,
                ParseCorrelationId(_requestContextAccessor.Current.CorrelationId),
                safeIp,
                request.UserAgent),
            cancellationToken);

        return VerifyMfaChallengeResponse.Success();
    }

    private static Guid? ParseCorrelationId(string? correlationId)
        => Guid.TryParse(correlationId, out Guid value) ? value : null;
}
