namespace SecureERP.Application.Modules.Workflow.DTOs;

public sealed record CreateApprovalInstanceResponse(
    bool IsSuccess,
    long? ApprovalInstanceId,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static CreateApprovalInstanceResponse Success(long id) => new(true, id, null, null);

    public static CreateApprovalInstanceResponse Failure(string errorCode, string errorMessage) =>
        new(false, null, errorCode, errorMessage);
}
