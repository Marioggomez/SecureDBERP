using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface IListSecurityEventsHandler
{
    Task<IReadOnlyList<SecurityEventDto>> HandleAsync(
        ListSecurityEventsRequest request,
        CancellationToken cancellationToken = default);
}
