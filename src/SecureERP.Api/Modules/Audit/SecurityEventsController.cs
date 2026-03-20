using Microsoft.AspNetCore.Mvc;
using SecureERP.Api.Modules.Security;
using SecureERP.Application.Modules.Security;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Api.Modules.Audit;

[ApiController]
[Route("api/v1/audit/security-events")]
public sealed class SecurityEventsController : ControllerBase
{
    private readonly IListSecurityEventsHandler _listSecurityEventsHandler;

    public SecurityEventsController(IListSecurityEventsHandler listSecurityEventsHandler)
    {
        _listSecurityEventsHandler = listSecurityEventsHandler;
    }

    [HttpGet]
    [RequirePermission(Permissions.AuditSecurityEventRead, true)]
    [ProducesResponseType(typeof(IReadOnlyList<SecurityEventContract>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SecurityEventContract>>> List(
        [FromQuery] int top = 100,
        [FromQuery] string? eventType = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? result = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SecurityEventDto> rows = await _listSecurityEventsHandler.HandleAsync(
            new ListSecurityEventsRequest(top, eventType, severity, result),
            cancellationToken);

        return Ok(rows.Select(item => new SecurityEventContract(
            item.SecurityEventId,
            item.UtcCreatedAt,
            item.EventType,
            item.Severity,
            item.Result,
            item.Detail,
            item.TenantId,
            item.CompanyId,
            item.UserId,
            item.SessionId,
            item.AuthFlowId,
            item.CorrelationId,
            item.IpAddress,
            item.UserAgent)).ToList());
    }
}
