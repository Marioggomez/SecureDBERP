using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Queries;

public sealed class GetSecurityUserByIdHandler : IGetSecurityUserByIdHandler
{
    private readonly ISecurityAdministrationRepository _repository;

    public GetSecurityUserByIdHandler(ISecurityAdministrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<SecurityUserDto?> HandleAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        SecurityUserSnapshot? item = await _repository.GetUserByIdAsync(userId, cancellationToken);
        return item is null ? null : ListSecurityUsersHandler.Map(item);
    }
}
