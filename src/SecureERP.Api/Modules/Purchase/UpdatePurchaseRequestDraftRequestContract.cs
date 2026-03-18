namespace SecureERP.Api.Modules.Purchase;

public sealed record UpdatePurchaseRequestDraftRequestContract(
    long? OrganizationUnitId,
    DateTime RequestDate,
    string? Notes);
