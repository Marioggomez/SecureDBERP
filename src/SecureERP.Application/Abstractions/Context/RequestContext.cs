namespace SecureERP.Application.Abstractions.Context;

public sealed record RequestContext(
    long? TenantId,
    long? CompanyId,
    long? UserId,
    Guid? SessionId,
    string? CorrelationId)
{
    public static RequestContext Empty { get; } = new(null, null, null, null, null);
}
