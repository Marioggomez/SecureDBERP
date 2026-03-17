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

    public VerifyMfaChallengeHandler(
        IAuthRepository authRepository,
        IMfaCodeService mfaCodeService,
        IRequestContextAccessor requestContextAccessor)
    {
        _authRepository = authRepository;
        _mfaCodeService = mfaCodeService;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<VerifyMfaChallengeResponse> HandleAsync(VerifyMfaChallengeRequest request, CancellationToken cancellationToken = default)
    {
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
                    _requestContextAccessor.Current.SessionId,
                    challenge.AuthFlowId,
                    ParseCorrelationId(_requestContextAccessor.Current.CorrelationId),
                    null,
                    null),
                cancellationToken);
            return VerifyMfaChallengeResponse.Failure("MFA_CODE_INVALID", "Invalid MFA code.");
        }

        await _authRepository.MarkMfaChallengeValidatedAsync(challenge.ChallengeId, cancellationToken);
        await _authRepository.MarkAuthFlowMfaValidatedAsync(challenge.AuthFlowId, cancellationToken);

        if (_requestContextAccessor.Current.SessionId is Guid sessionId)
        {
            await _authRepository.MarkSessionMfaValidatedAsync(sessionId, cancellationToken);
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
                _requestContextAccessor.Current.SessionId,
                challenge.AuthFlowId,
                ParseCorrelationId(_requestContextAccessor.Current.CorrelationId),
                null,
                null),
            cancellationToken);

        return VerifyMfaChallengeResponse.Success();
    }

    private static Guid? ParseCorrelationId(string? correlationId)
        => Guid.TryParse(correlationId, out Guid value) ? value : null;
}
