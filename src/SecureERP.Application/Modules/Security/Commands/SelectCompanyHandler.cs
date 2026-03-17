using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Commands;

public sealed class SelectCompanyHandler : ISelectCompanyHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public SelectCompanyHandler(
        IAuthRepository authRepository,
        ITokenGenerator tokenGenerator,
        IRequestContextAccessor requestContextAccessor)
    {
        _authRepository = authRepository;
        _tokenGenerator = tokenGenerator;
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task<SelectCompanyResponse> HandleAsync(SelectCompanyRequest request, CancellationToken cancellationToken = default)
    {
        AuthFlowSnapshot? flow = await _authRepository.GetAuthFlowAsync(request.AuthFlowId, cancellationToken);
        if (flow is null)
        {
            return SelectCompanyResponse.Failure("AUTH_FLOW_NOT_FOUND", "Authentication flow was not found.");
        }

        if (flow.IsUsed)
        {
            return SelectCompanyResponse.Failure("AUTH_FLOW_ALREADY_USED", "Authentication flow is already used.");
        }

        if (flow.UtcExpiresAt < DateTime.UtcNow)
        {
            return SelectCompanyResponse.Failure("AUTH_FLOW_EXPIRED", "Authentication flow is expired.");
        }

        IReadOnlyList<OperableCompany> companies = await _authRepository.GetOperableCompaniesAsync(
            flow.UserId,
            flow.TenantId,
            cancellationToken);

        if (!companies.Any(c => c.CompanyId == request.CompanyId))
        {
            return SelectCompanyResponse.Failure("AUTH_COMPANY_NOT_ALLOWED", "Company is not allowed for this user.");
        }

        Guid sessionId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset absoluteExpiration = now.AddHours(8);
        string opaqueToken = _tokenGenerator.GenerateOpaqueToken();
        byte[] tokenHash = _tokenGenerator.ComputeSha256(opaqueToken);

        RequestContext currentContext = _requestContextAccessor.Current;
        _requestContextAccessor.SetCurrent(new RequestContext(
            flow.TenantId,
            request.CompanyId,
            flow.UserId,
            sessionId,
            currentContext.CorrelationId));

        await _authRepository.CreateSessionAsync(
            new UserSessionToCreate(
                sessionId,
                flow.UserId,
                flow.TenantId,
                request.CompanyId,
                tokenHash,
                "LOGIN",
                flow.MfaValidated,
                now.UtcDateTime,
                absoluteExpiration.UtcDateTime,
                now.UtcDateTime,
                request.IpAddress,
                request.UserAgent),
            cancellationToken);

        bool flowMarked = await _authRepository.MarkAuthFlowAsUsedAsync(
            flow.AuthFlowId,
            flow.MfaValidated,
            cancellationToken);

        if (!flowMarked)
        {
            return SelectCompanyResponse.Failure("AUTH_FLOW_UPDATE_FAILED", "Authentication flow could not be finalized.");
        }

        await _authRepository.WriteSecurityEventAsync(
            new SecurityEventToCreate(
                "AUTH_SELECT_COMPANY_SUCCESS",
                "INFO",
                "OK",
                "Session created successfully.",
                flow.TenantId,
                request.CompanyId,
                flow.UserId,
                sessionId,
                flow.AuthFlowId,
                ParseCorrelationId(currentContext.CorrelationId),
                request.IpAddress,
                request.UserAgent),
            cancellationToken);

        return SelectCompanyResponse.Success(
            opaqueToken,
            sessionId,
            absoluteExpiration,
            flow.UserId,
            flow.TenantId,
            request.CompanyId);
    }

    private static Guid? ParseCorrelationId(string? correlationId)
    {
        return Guid.TryParse(correlationId, out Guid value) ? value : null;
    }
}
