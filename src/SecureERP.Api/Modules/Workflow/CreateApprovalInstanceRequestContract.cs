namespace SecureERP.Api.Modules.Workflow;

public sealed record CreateApprovalInstanceRequestContract(
    long OrganizationUnitId,
    long ApprovalProfileId,
    string EntityCode,
    long ObjectId,
    byte CurrentLevel,
    short ApprovalStateId,
    DateTime? UtcExpiresAt,
    string? Reason,
    string Payload);
