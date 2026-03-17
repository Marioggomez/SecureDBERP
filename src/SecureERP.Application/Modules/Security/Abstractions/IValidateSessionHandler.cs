using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface IValidateSessionHandler
{
    Task<ValidateSessionResult> HandleAsync(ValidateSessionRequest request, CancellationToken cancellationToken = default);
}
