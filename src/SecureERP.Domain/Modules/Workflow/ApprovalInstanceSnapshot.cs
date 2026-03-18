namespace SecureERP.Domain.Modules.Workflow;

public sealed record ApprovalInstanceSnapshot(
    long ApprovalInstanceId,
    long TenantId,
    long CompanyId,
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
