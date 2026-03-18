namespace SecureERP.Domain.Modules.Workflow;

public interface IWorkflowPilotRepository
{
    Task<IReadOnlyList<ApprovalInstanceSnapshot>> ListApprovalInstancesAsync(CancellationToken cancellationToken = default);

    Task<long> CreateApprovalInstanceAsync(
        ApprovalInstanceToCreate instance,
        CancellationToken cancellationToken = default);
}
