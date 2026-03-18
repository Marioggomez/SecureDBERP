using SecureERP.Application.Modules.Workflow.DTOs;

namespace SecureERP.Application.Modules.Workflow.Abstractions;

public interface IListApprovalInstancesHandler
{
    Task<IReadOnlyList<ApprovalInstanceDto>> HandleAsync(CancellationToken cancellationToken = default);
}
