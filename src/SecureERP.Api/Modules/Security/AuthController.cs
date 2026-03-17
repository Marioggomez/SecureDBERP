using Microsoft.AspNetCore.Mvc;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Api.Modules.Security;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ILoginHandler _loginHandler;

    public AuthController(ILoginHandler loginHandler)
    {
        _loginHandler = loginHandler;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponseContract), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseContract>> Login([FromBody] LoginRequestContract request, CancellationToken cancellationToken)
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        string? userAgent = Request.Headers.UserAgent.ToString();

        LoginRequest applicationRequest = new(
            request.TenantCode,
            request.Identifier,
            request.Password,
            ipAddress,
            userAgent);

        LoginResponse result = await _loginHandler.HandleAsync(applicationRequest, cancellationToken);
        LoginResponseContract response = new(
            result.IsAuthenticated,
            result.AccessToken,
            result.SessionId,
            result.ExpiresAtUtc,
            result.UserId,
            result.TenantId,
            result.CompanyId,
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
}
