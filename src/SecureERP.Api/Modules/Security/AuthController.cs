using Microsoft.AspNetCore.Mvc;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Api.Modules.Security;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ILoginHandler _loginHandler;
    private readonly ISelectCompanyHandler _selectCompanyHandler;
    private readonly IValidateSessionHandler _validateSessionHandler;
    private readonly IRequestMfaChallengeHandler _requestMfaChallengeHandler;
    private readonly IVerifyMfaChallengeHandler _verifyMfaChallengeHandler;

    public AuthController(
        ILoginHandler loginHandler,
        ISelectCompanyHandler selectCompanyHandler,
        IValidateSessionHandler validateSessionHandler,
        IRequestMfaChallengeHandler requestMfaChallengeHandler,
        IVerifyMfaChallengeHandler verifyMfaChallengeHandler)
    {
        _loginHandler = loginHandler;
        _selectCompanyHandler = selectCompanyHandler;
        _validateSessionHandler = validateSessionHandler;
        _requestMfaChallengeHandler = requestMfaChallengeHandler;
        _verifyMfaChallengeHandler = verifyMfaChallengeHandler;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponseContract), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseContract>> Login([FromBody] LoginRequestContract request, CancellationToken cancellationToken)
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        string? userAgent = Request.Headers.UserAgent.ToString();

        LoginResponse result = await _loginHandler.HandleAsync(
            new LoginRequest(
                request.TenantCode,
                request.Identifier,
                request.Password,
                ipAddress,
                userAgent),
            cancellationToken);

        LoginResponseContract response = new(
            result.IsAuthenticated,
            result.AuthFlowId,
            result.UserId,
            result.TenantId,
            result.OperableCompanies.Select(c => new OperableCompanyContract(
                c.CompanyId,
                c.CompanyCode,
                c.CompanyName,
                c.IsDefault)).ToList(),
            result.RequiresPasswordChange,
            result.RequiresMfa,
            result.ErrorCode,
            result.ErrorMessage);

        if (!result.IsAuthenticated)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    [HttpPost("select-company")]
    [ProducesResponseType(typeof(SelectCompanyResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SelectCompanyResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SelectCompanyResponseContract>> SelectCompany(
        [FromBody] SelectCompanyRequestContract request,
        CancellationToken cancellationToken)
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        string? userAgent = Request.Headers.UserAgent.ToString();

        SelectCompanyResponse result = await _selectCompanyHandler.HandleAsync(
            new SelectCompanyRequest(
                request.AuthFlowId,
                request.CompanyId,
                ipAddress,
                userAgent),
            cancellationToken);

        SelectCompanyResponseContract response = new(
            result.IsSuccess,
            result.AccessToken,
            result.SessionId,
            result.ExpiresAtUtc,
            result.UserId,
            result.TenantId,
            result.CompanyId,
            result.ErrorCode,
            result.ErrorMessage);

        if (!result.IsSuccess)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("validate-session")]
    [ProducesResponseType(typeof(ValidateSessionResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidateSessionResponseContract), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ValidateSessionResponseContract>> ValidateSession(
        [FromBody] ValidateSessionRequestContract request,
        CancellationToken cancellationToken)
    {
        ValidateSessionResult result = await _validateSessionHandler.HandleAsync(
            new ValidateSessionRequest(
                request.AccessToken,
                request.IdleTimeoutMinutes,
                request.UpdateLastActivity),
            cancellationToken);

        ValidateSessionResponseContract response = new(
            result.IsValid,
            result.SessionId,
            result.UserId,
            result.TenantId,
            result.CompanyId,
            result.MfaValidated,
            result.ExpiresAtUtc,
            result.ErrorCode,
            result.ErrorMessage);

        if (!result.IsValid)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    [HttpPost("mfa/challenge")]
    [ProducesResponseType(typeof(RequestMfaChallengeResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestMfaChallengeResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequestMfaChallengeResponseContract>> RequestMfaChallenge(
        [FromBody] RequestMfaChallengeRequestContract request,
        CancellationToken cancellationToken)
    {
        RequestMfaChallengeResponse result = await _requestMfaChallengeHandler.HandleAsync(
            new RequestMfaChallengeRequest(
                request.AuthFlowId,
                (MfaPurpose)request.Purpose,
                (MfaChannel)request.Channel,
                request.ActionCode),
            cancellationToken);

        RequestMfaChallengeResponseContract response = new(
            result.IsSuccess,
            result.ChallengeId,
            result.ExpiresAtUtc,
            result.DeliveryHint,
            result.ErrorCode,
            result.ErrorMessage);

        if (!result.IsSuccess)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("mfa/verify")]
    [ProducesResponseType(typeof(VerifyMfaChallengeResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(VerifyMfaChallengeResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerifyMfaChallengeResponseContract>> VerifyMfaChallenge(
        [FromBody] VerifyMfaChallengeRequestContract request,
        CancellationToken cancellationToken)
    {
        VerifyMfaChallengeResponse result = await _verifyMfaChallengeHandler.HandleAsync(
            new VerifyMfaChallengeRequest(request.ChallengeId, request.OtpCode),
            cancellationToken);

        VerifyMfaChallengeResponseContract response = new(
            result.IsVerified,
            result.ErrorCode,
            result.ErrorMessage);

        if (!result.IsVerified)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
