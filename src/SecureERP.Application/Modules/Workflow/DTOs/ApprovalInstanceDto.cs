namespace SecureERP.Application.Modules.Workflow.DTOs;

public sealed record ApprovalInstanceDto(
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
    bool IsActive);
