namespace SecureERP.Application.Modules.Security;

public static class Permissions
{
    public const string AuthSessionValidate = "AUTH.SESSION.VALIDATE";
    public const string AuthSessionRevoke = "AUTH.SESSION.REVOKE";
    public const string AuthMfaChallenge = "AUTH.MFA.CHALLENGE";
    public const string AuthMfaVerify = "AUTH.MFA.VERIFY";

    public const string SecurityUserRead = "SECURITY.USER.READ";
    public const string SecurityUserResetPassword = "SECURITY.USER.RESET_PASSWORD";
    public const string SecurityRoleAssign = "SECURITY.ROLE.ASSIGN";

    public const string OrganizationUnitRead = "ORGANIZATION.UNIT.READ";
    public const string OrganizationUnitCreate = "ORGANIZATION.UNIT.CREATE";

    public const string WorkflowApprovalInstanceRead = "WORKFLOW.APPROVAL_INSTANCE.READ";
    public const string WorkflowApprovalInstanceCreate = "WORKFLOW.APPROVAL_INSTANCE.CREATE";

    public const string SystemHealthRead = "SYSTEM.HEALTH.READ";
    public const string AuditSecurityEventRead = "AUDIT.SECURITY_EVENT.READ";

    public static readonly IReadOnlyList<string> All =
    [
        AuthSessionValidate,
        AuthSessionRevoke,
        AuthMfaChallenge,
        AuthMfaVerify,
        SecurityUserRead,
        SecurityUserResetPassword,
        SecurityRoleAssign,
        OrganizationUnitRead,
        OrganizationUnitCreate,
        WorkflowApprovalInstanceRead,
        WorkflowApprovalInstanceCreate,
        SystemHealthRead,
        AuditSecurityEventRead
    ];
}
