using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface ILoginHandler
{
    Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
