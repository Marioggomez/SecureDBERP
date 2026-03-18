using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Queries;

public sealed class ValidateSessionHandler : IValidateSessionHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IOperationalSecurityService _operationalSecurityService;

    public ValidateSessionHandler(
        IAuthRepository authRepository,
        ITokenGenerator tokenGenerator,
        IOperationalSecurityService operationalSecurityService)
    {
        _authRepository = authRepository;
        _tokenGenerator = tokenGenerator;
        _operationalSecurityService = operationalSecurityService;
    }

    public async Task<ValidateSessionResult> HandleAsync(ValidateSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return ValidateSessionResult.Failure("SESSION_TOKEN_REQUIRED", "Access token is required.");
        }

        OperationalSecurityDecision guard = await _operationalSecurityService.GuardAsync(
            "AUTH.VALIDATE_SESSION",
            request.IpAddress,
            null,
            null,
            null,
            cancellationToken);
        if (!guard.IsAllowed)
        {
            string eventType = guard.Code.StartsWith("IP_", StringComparison.OrdinalIgnoreCase)
                ? "IP_POLICY_DENY"
                : "VALIDATE_SESSION_RATE_LIMIT_HIT";
            await _authRepository.WriteSecurityEventAsync(
                new SecurityEventToCreate(
                    eventType,
                    "WARNING",
                    "DENIED",
                    $"Validate-session blocked by operational policy ({guard.Code}).",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    request.IpAddress,
                    null),
                cancellationToken);
            return ValidateSessionResult.Failure("AUTH_REQUEST_REJECTED", "Operation rejected.");
        }

        byte[] tokenHash = _tokenGenerator.ComputeSha256(request.AccessToken.Trim());

        SessionValidationSnapshot? session = await _authRepository.ValidateSessionByTokenHashAsync(
            tokenHash,
            request.IdleTimeoutMinutes,
            request.UpdateLastActivity,
            cancellationToken);

        if (session is null || !session.IsSessionValid)
        {
            return ValidateSessionResult.Failure("SESSION_INVALID", "Session is invalid or expired.");
        }

        return ValidateSessionResult.Success(
            session.SessionId,
            session.UserId,
            session.TenantId,
            session.CompanyId,
            session.MfaValidated,
            new DateTimeOffset(session.UtcAbsoluteExpiration, TimeSpan.Zero));
    }
}
