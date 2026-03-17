using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;
using System.Security.Cryptography;
using System.Text;

namespace SecureERP.Application.Modules.Security.Commands;

public sealed class LoginHandler : ILoginHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public LoginHandler(
        IAuthRepository authRepository,
        IPasswordHasher passwordHasher,
        IRequestContextAccessor requestContextAccessor)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TenantCode) ||
            string.IsNullOrWhiteSpace(request.Identifier) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return LoginResponse.Failure("LOGIN_INVALID_REQUEST", "TenantCode, identifier and password are required.");
        }

        LoginUserCredential? user = await _authRepository.GetUserForLoginAsync(
            request.TenantCode.Trim(),
            request.Identifier.Trim(),
            cancellationToken);

        if (user is null)
        {
            return LoginResponse.Failure("LOGIN_INVALID_CREDENTIALS", "Invalid credentials.");
        }

        if (!user.IsActiveUser || !user.IsCredentialActive)
        {
            return LoginResponse.Failure("LOGIN_USER_INACTIVE", "User is inactive.");
        }

        if (user.CompanyId <= 0)
        {
            return LoginResponse.Failure("LOGIN_NO_COMPANY_SCOPE", "User has no operable company scope.");
        }

        bool passwordMatches = _passwordHasher.VerifyPassword(
            request.Password,
            user.PasswordHash,
            user.PasswordSalt,
            user.PasswordAlgorithm,
            user.PasswordIterations);

        if (!passwordMatches)
        {
            return LoginResponse.Failure("LOGIN_INVALID_CREDENTIALS", "Invalid credentials.");
        }

        if (user.MfaEnabled)
        {
            return LoginResponse.Failure("LOGIN_MFA_REQUIRED", "MFA is required for this account.", true);
        }

        Guid sessionId = Guid.NewGuid();
        string accessToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        byte[] tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(accessToken));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset expiresAt = now.AddHours(8);

        RequestContext currentContext = _requestContextAccessor.Current;
        _requestContextAccessor.SetCurrent(new RequestContext(
            user.TenantId,
            user.CompanyId,
            user.UserId,
            sessionId,
            currentContext.CorrelationId));

        UserSessionToCreate session = new(
            sessionId,
            user.UserId,
            user.TenantId,
            user.CompanyId,
            tokenHash,
            "LOGIN",
            false,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            now.UtcDateTime,
            request.IpAddress,
            request.UserAgent);

        await _authRepository.CreateSessionAsync(session, cancellationToken);

        return LoginResponse.Success(
            accessToken,
            sessionId,
            expiresAt,
            user.UserId,
            user.TenantId,
            user.CompanyId,
            user.RequiresPasswordChange);
    }
}
