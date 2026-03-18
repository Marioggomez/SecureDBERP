using SecureERP.Application.Modules.Workflow.DTOs;

namespace SecureERP.Application.Modules.Workflow.Abstractions;

public interface ICreateApprovalInstanceHandler
{
    Task<CreateApprovalInstanceResponse> HandleAsync(
        CreateApprovalInstanceRequest request,
        CancellationToken cancellationToken = default);
}
