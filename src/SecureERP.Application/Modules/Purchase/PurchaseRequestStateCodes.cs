using SecureERP.Domain.Modules.Purchase;

namespace SecureERP.Application.Modules.Purchase;

internal static class PurchaseRequestStateCodes
{
    public static string ToCode(PurchaseRequestState state) => state switch
    {
        PurchaseRequestState.Draft => "DRAFT",
        PurchaseRequestState.Submitted => "SUBMITTED",
        PurchaseRequestState.Approved => "APPROVED",
        PurchaseRequestState.Rejected => "REJECTED",
        PurchaseRequestState.Cancelled => "CANCELLED",
        _ => "UNKNOWN"
    };
}
