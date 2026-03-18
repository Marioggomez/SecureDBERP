namespace SecureERP.Application.Modules.Workflow.DTOs;

public sealed record CreateApprovalInstanceRequest(
    long OrganizationUnitId,
    long ApprovalProfileId,
    string EntityCode,
    long ObjectId,
    byte CurrentLevel,
    short ApprovalStateId,
    DateTime? UtcExpiresAt,
    string? Reason,
    string Payload);
