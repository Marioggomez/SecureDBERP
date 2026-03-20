using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Queries;

public sealed class ListSecurityUsersHandler : IListSecurityUsersHandler
{
    private readonly ISecurityAdministrationRepository _repository;

    public ListSecurityUsersHandler(ISecurityAdministrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SecurityUserDto>> HandleAsync(
        ListSecurityUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SecurityUserSnapshot> data = await _repository.ListUsersAsync(
            request.Search,
            request.ActiveOnly,
            cancellationToken);

        return data.Select(Map).ToList();
    }

    internal static SecurityUserDto Map(SecurityUserSnapshot item)
    {
        return new SecurityUserDto(
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
            item.CompanyScopeStartUtc,
            item.CompanyScopeEndUtc,
            item.BlockedUntilUtc,
            item.LastAccessUtc);
    }
}
