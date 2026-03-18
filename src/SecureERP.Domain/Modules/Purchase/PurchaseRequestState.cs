namespace SecureERP.Domain.Modules.Purchase;

public enum PurchaseRequestState : short
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5
}
