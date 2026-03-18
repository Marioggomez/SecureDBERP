namespace SecureERP.Api.Modules.Workflow;

public sealed record CreateApprovalInstanceResponseContract(
    bool IsSuccess,
    long? ApprovalInstanceId,
    string? ErrorCode,
    string? ErrorMessage);
