using Microsoft.AspNetCore.Mvc;
using SecureERP.Application.Modules.Security;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Api.Modules.Security;

[ApiController]
[Route("api/v1/security")]
public sealed class SecurityAdministrationController : ControllerBase
{
    private readonly IListSecurityUsersHandler _listSecurityUsersHandler;
    private readonly IGetSecurityUserByIdHandler _getSecurityUserByIdHandler;
    private readonly IRevokeSessionHandler _revokeSessionHandler;

    public SecurityAdministrationController(
        IListSecurityUsersHandler listSecurityUsersHandler,
        IGetSecurityUserByIdHandler getSecurityUserByIdHandler,
        IRevokeSessionHandler revokeSessionHandler)
    {
        _listSecurityUsersHandler = listSecurityUsersHandler;
        _getSecurityUserByIdHandler = getSecurityUserByIdHandler;
        _revokeSessionHandler = revokeSessionHandler;
    }

    [HttpGet("users")]
    [RequirePermission(Permissions.SecurityUserRead)]
    [ProducesResponseType(typeof(IReadOnlyList<SecurityUserListItemContract>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SecurityUserListItemContract>>> ListUsers(
        [FromQuery] string? search,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SecurityUserDto> result = await _listSecurityUsersHandler.HandleAsync(
            new ListSecurityUsersRequest(search, activeOnly),
            cancellationToken);

        return Ok(result.Select(item => new SecurityUserListItemContract(
            item.UserId,
            item.Code,
            item.Login,
            item.DisplayName,
            item.Email,
            item.MfaEnabled,
            item.RequiresPasswordChange,
            item.IsActive,
            item.IsTenantAdministrator,
            item.CompanyId,
            item.IsDefaultCompany,
            item.CanOperateCompany,
            item.BlockedUntilUtc,
            item.LastAccessUtc)).ToList());
    }

    [HttpGet("users/{id:long}")]
    [RequirePermission(Permissions.SecurityUserRead)]
    [ProducesResponseType(typeof(SecurityUserContract), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SecurityUserContract>> GetUserById(
        long id,
        CancellationToken cancellationToken = default)
    {
        SecurityUserDto? result = await _getSecurityUserByIdHandler.HandleAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(new SecurityUserContract(
            result.UserId,
            result.Code,
            result.Login,
            result.DisplayName,
            result.Email,
            result.MfaEnabled,
            result.RequiresPasswordChange,
            result.IsActive,
            result.IsTenantAdministrator,
            result.CompanyId,
            result.IsDefaultCompany,
            result.CanOperateCompany,
            result.CompanyScopeStartUtc,
            result.CompanyScopeEndUtc,
            result.BlockedUntilUtc,
            result.LastAccessUtc));
    }

    [HttpPost("sessions/{sessionId:guid}/revoke")]
    [RequirePermission(Permissions.AuthSessionRevoke, true)]
    [ProducesResponseType(typeof(RevokeSessionResponseContract), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RevokeSessionResponseContract), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RevokeSessionResponseContract>> RevokeSession(
        Guid sessionId,
        [FromBody] RevokeSessionRequestContract request,
        CancellationToken cancellationToken = default)
    {
        RevokeSessionResponse result = await _revokeSessionHandler.HandleAsync(
            new RevokeSessionRequest(
                sessionId,
                request.Reason,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        RevokeSessionResponseContract response = new(
            result.IsSuccess,
            result.ErrorCode,
            result.ErrorMessage);

        return result.IsSuccess ? Ok(response) : BadRequest(response);
    }
}
