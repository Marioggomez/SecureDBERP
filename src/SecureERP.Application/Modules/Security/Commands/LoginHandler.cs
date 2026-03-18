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
    private readonly IOperationalSecurityService _operationalSecurityService;

    public LoginHandler(
        IAuthRepository authRepository,
        IPasswordHasher passwordHasher,
        IRequestContextAccessor requestContextAccessor,
        IOperationalSecurityService operationalSecurityService)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _requestContextAccessor = requestContextAccessor;
        _operationalSecurityService = operationalSecurityService;
    }

    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TenantCode) ||
            string.IsNullOrWhiteSpace(request.Identifier) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return LoginResponse.Failure("AUTH_REQUEST_REJECTED", "Authentication request rejected.");
        }

        string identifier = request.Identifier.Trim();
        string tenantCode = request.TenantCode.Trim();
        string safeIp = string.IsNullOrWhiteSpace(request.IpAddress) ? "0.0.0.0" : request.IpAddress.Trim();

        OperationalSecurityDecision guard = await _operationalSecurityService.GuardAsync(
            "AUTH.LOGIN",
            safeIp,
            $"{tenantCode}:{identifier}".ToUpperInvariant(),
            null,
            null,
            cancellationToken);
        if (!guard.IsAllowed)
        {
            string eventType = guard.Code.StartsWith("IP_", StringComparison.OrdinalIgnoreCase)
                ? "IP_POLICY_DENY"
                : "RATE_LIMIT_HIT";
            await _authRepository.WriteSecurityEventAsync(
                new SecurityEventToCreate(
                    eventType,
                    "WARNING",
                    "DENIED",
                    $"Login blocked by operational policy ({guard.Code}).",
                    null,
                    null,
                    null,
                    null,
                    null,
                    ParseCorrelationId(_requestContextAccessor.Current.CorrelationId),
                    safeIp,
                    request.UserAgent),
                cancellationToken);

            return LoginResponse.Failure("AUTH_REQUEST_REJECTED", "Authentication request rejected.");
        }

        OperationalLockoutDecision lockout = await _operationalSecurityService.CheckLoginLockoutAsync(
            identifier,
            safeIp,
            cancellationToken);
        if (lockout.IsLocked)
        {
            await _authRepository.WriteSecurityEventAsync(
                new SecurityEventToCreate(
                    "LOGIN_LOCKOUT",
                    "WARNING",
                    "DENIED",
                    "Login blocked due to lockout policy.",
                    null,
                    null,
                    null,
                    null,
                    null,
                    ParseCorrelationId(_requestContextAccessor.Current.CorrelationId),
                    safeIp,
                    request.UserAgent),
                cancellationToken);

            return LoginResponse.Failure("AUTH_REQUEST_REJECTED", "Authentication request rejected.");
        }

        LoginUserCredential? user = await _authRepository.GetUserForLoginAsync(
            tenantCode,
            identifier,
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

            await _operationalSecurityService.RegisterLoginFailureAsync(identifier, safeIp, cancellationToken);
            return LoginResponse.Failure("AUTH_REQUEST_REJECTED", "Authentication request rejected.");
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

            OperationalLockoutDecision failure = await _operationalSecurityService.RegisterLoginFailureAsync(identifier, safeIp, cancellationToken);
            if (failure.IsLocked)
            {
                await _authRepository.WriteSecurityEventAsync(
                    new SecurityEventToCreate(
                        "LOGIN_LOCKOUT",
                        "WARNING",
                        "DENIED",
                        "Login lockout activated due to failed attempts.",
                        user.TenantId,
                        null,
                        user.UserId,
                        null,
                        null,
                        ParseCorrelationId(_requestContextAccessor.Current.CorrelationId),
                        safeIp,
                        request.UserAgent),
                    cancellationToken);
            }

            return LoginResponse.Failure("AUTH_REQUEST_REJECTED", "Authentication request rejected.");
        }

        await _operationalSecurityService.RegisterLoginSuccessAsync(identifier, safeIp, cancellationToken);

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
