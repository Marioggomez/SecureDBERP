using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface ISelectCompanyHandler
{
    Task<SelectCompanyResponse> HandleAsync(SelectCompanyRequest request, CancellationToken cancellationToken = default);
}
