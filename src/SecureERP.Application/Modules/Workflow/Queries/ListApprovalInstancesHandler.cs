using SecureERP.Application.Modules.Workflow.Abstractions;
using SecureERP.Application.Modules.Workflow.DTOs;
using SecureERP.Domain.Modules.Workflow;

namespace SecureERP.Application.Modules.Workflow.Queries;

public sealed class ListApprovalInstancesHandler : IListApprovalInstancesHandler
{
    private readonly IWorkflowPilotRepository _repository;

    public ListApprovalInstancesHandler(IWorkflowPilotRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ApprovalInstanceDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApprovalInstanceSnapshot> data = await _repository.ListApprovalInstancesAsync(cancellationToken);
        return data.Select(item => new ApprovalInstanceDto(
            item.ApprovalInstanceId,
            item.TenantId,
            item.CompanyId,
            item.OrganizationUnitId,
            item.ApprovalProfileId,
            item.EntityCode,
            item.ObjectId,
            item.CurrentLevel,
            item.ApprovalStateId,
            item.RequestedByUserId,
            item.UtcRequestedAt,
            item.UtcExpiresAt,
            item.Reason,
            item.IsActive)).ToList();
    }
}
