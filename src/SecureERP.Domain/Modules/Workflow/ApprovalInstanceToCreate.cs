namespace SecureERP.Domain.Modules.Workflow;

public sealed record ApprovalInstanceToCreate(
    long OrganizationUnitId,
    long ApprovalProfileId,
    string EntityCode,
    long ObjectId,
    byte CurrentLevel,
    short ApprovalStateId,
    long RequestedByUserId,
    DateTime UtcRequestedAt,
    DateTime? UtcExpiresAt,
    string? Reason,
    byte[] PayloadHash,
    bool IsActive);
