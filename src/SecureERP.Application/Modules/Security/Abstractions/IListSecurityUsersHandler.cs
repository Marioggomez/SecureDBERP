using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface IListSecurityUsersHandler
{
    Task<IReadOnlyList<SecurityUserDto>> HandleAsync(
        ListSecurityUsersRequest request,
        CancellationToken cancellationToken = default);
}
