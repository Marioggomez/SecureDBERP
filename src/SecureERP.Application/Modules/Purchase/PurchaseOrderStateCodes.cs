using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase;

internal static class PurchaseOrderStateCodes
{
    public static string ToCode(PurchaseOrderState state) => state switch
    {
        PurchaseOrderState.Draft => "DRAFT",
        PurchaseOrderState.Submitted => "SUBMITTED",
        PurchaseOrderState.Approved => "APPROVED",
        PurchaseOrderState.Rejected => "REJECTED",
        PurchaseOrderState.Cancelled => "CANCELLED",
        _ => "UNKNOWN"
    };
}


