using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface IGetSecurityUserByIdHandler
{
    Task<SecurityUserDto?> HandleAsync(
        long userId,
        CancellationToken cancellationToken = default);
}
