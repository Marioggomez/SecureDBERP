using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Queries;

public sealed class ListSecurityEventsHandler : IListSecurityEventsHandler
{
    private readonly ISecurityAdministrationRepository _repository;

    public ListSecurityEventsHandler(ISecurityAdministrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SecurityEventDto>> HandleAsync(
        ListSecurityEventsRequest request,
        CancellationToken cancellationToken = default)
    {
        int top = request.Top <= 0 ? 100 : Math.Min(request.Top, 500);
        IReadOnlyList<SecurityEventSnapshot> data = await _repository.ListSecurityEventsAsync(
            top,
            request.EventType,
            request.Severity,
            request.Result,
            cancellationToken);

        return data.Select(item => new SecurityEventDto(
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
            item.UserAgent)).ToList();
    }
}
