using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Queries;

public sealed class ValidateSessionHandler : IValidateSessionHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenGenerator _tokenGenerator;

    public ValidateSessionHandler(IAuthRepository authRepository, ITokenGenerator tokenGenerator)
    {
        _authRepository = authRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<ValidateSessionResult> HandleAsync(ValidateSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return ValidateSessionResult.Failure("SESSION_TOKEN_REQUIRED", "Access token is required.");
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
