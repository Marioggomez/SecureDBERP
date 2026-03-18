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
    public const string PurchaseRequestRead = "PURCHASE.REQUEST.READ";
    public const string PurchaseRequestCreate = "PURCHASE.REQUEST.CREATE";
    public const string PurchaseRequestUpdate = "PURCHASE.REQUEST.UPDATE";
    public const string PurchaseRequestSubmit = "PURCHASE.REQUEST.SUBMIT";
    public const string PurchaseRequestApprove = "PURCHASE.REQUEST.APPROVE";
    public const string PurchaseOrderRead = "PURCHASE.ORDER.READ";
    public const string PurchaseOrderCreate = "PURCHASE.ORDER.CREATE";
    public const string PurchaseOrderUpdate = "PURCHASE.ORDER.UPDATE";
    public const string PurchaseOrderSubmit = "PURCHASE.ORDER.SUBMIT";
    public const string PurchaseOrderApprove = "PURCHASE.ORDER.APPROVE";

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
        PurchaseRequestRead,
        PurchaseRequestCreate,
        PurchaseRequestUpdate,
        PurchaseRequestSubmit,
        PurchaseRequestApprove,
        PurchaseOrderRead,
        PurchaseOrderCreate,
        PurchaseOrderUpdate,
        PurchaseOrderSubmit,
        PurchaseOrderApprove,
        SystemHealthRead,
        AuditSecurityEventRead
    ];
}
