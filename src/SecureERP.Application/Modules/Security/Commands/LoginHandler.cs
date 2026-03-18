using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

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
            await _authRepository.WriteSecurityEventAsync(
                new SecurityEventToCreate(
                    "AUTH_LOGIN_FAILED",
                    "WARNING",
                    "DENIED",
                    "Invalid credentials.",
                    null,
                    null,
                    null,
                    null,
                    null,
                    ParseCorrelationId(_requestContextAccessor.Current.CorrelationId),
                    request.IpAddress,
                    request.UserAgent),
                cancellationToken);

            return LoginResponse.Failure("LOGIN_INVALID_CREDENTIALS", "Invalid credentials.");
        }

        if (!user.IsActiveUser || !user.IsCredentialActive)
        {
            return LoginResponse.Failure("LOGIN_USER_INACTIVE", "User is inactive.");
        }

        bool passwordMatches = _passwordHasher.VerifyPassword(
            request.Password,
            user.PasswordHash,
            user.PasswordSalt,
            user.PasswordAlgorithm,
            user.PasswordIterations);

        if (!passwordMatches)
        {
            await _authRepository.WriteSecurityEventAsync(
                new SecurityEventToCreate(
                    "AUTH_LOGIN_FAILED",
                    "WARNING",
                    "DENIED",
                    "Invalid credentials.",
                    user.TenantId,
                    null,
                    user.UserId,
                    null,
                    null,
                    ParseCorrelationId(_requestContextAccessor.Current.CorrelationId),
                    request.IpAddress,
                    request.UserAgent),
                cancellationToken);

            return LoginResponse.Failure("LOGIN_INVALID_CREDENTIALS", "Invalid credentials.");
        }

        RequestContext currentContext = _requestContextAccessor.Current;
        _requestContextAccessor.SetCurrent(new RequestContext(
            user.TenantId,
            user.CompanyId == 0 ? null : user.CompanyId,
            user.UserId,
            null,
            currentContext.CorrelationId));

        IReadOnlyList<OperableCompany> companies = await _authRepository.GetOperableCompaniesAsync(
            user.UserId,
            user.TenantId,
            cancellationToken);

        if (companies.Count == 0)
        {
            return LoginResponse.Failure("LOGIN_NO_COMPANY_SCOPE", "User has no operable company scope.");
        }

        Guid authFlowId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        _requestContextAccessor.SetCurrent(new RequestContext(
            user.TenantId,
            null,
            user.UserId,
            null,
            currentContext.CorrelationId));

        await _authRepository.CreateAuthFlowAsync(
            new AuthFlowToCreate(
                authFlowId,
                user.UserId,
                user.TenantId,
                user.MfaEnabled,
                false,
                now.AddMinutes(10),
                request.IpAddress,
                request.UserAgent,
                currentContext.CorrelationId),
            cancellationToken);

        await _authRepository.WriteSecurityEventAsync(
            new SecurityEventToCreate(
                "AUTH_LOGIN_SUCCESS",
                "INFO",
                "OK",
                "Login validated. Pending company selection.",
                user.TenantId,
                null,
                user.UserId,
                null,
                authFlowId,
                ParseCorrelationId(currentContext.CorrelationId),
                request.IpAddress,
                request.UserAgent),
            cancellationToken);

        IReadOnlyList<OperableCompanyDto> companyDtos = companies
            .Select(c => new OperableCompanyDto(c.CompanyId, c.CompanyCode, c.CompanyName, c.IsDefault))
            .ToList();

        return LoginResponse.Success(
            authFlowId,
            user.UserId,
            user.TenantId,
            companyDtos,
            user.RequiresPasswordChange,
            user.MfaEnabled);
    }

    private static Guid? ParseCorrelationId(string? correlationId)
    {
        return Guid.TryParse(correlationId, out Guid value) ? value : null;
    }
}
