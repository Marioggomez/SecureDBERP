namespace SecureERP.Infrastructure.Auditing;

public sealed class NoOpAuditTrailWriter : IAuditTrailWriter
{
    public Task WriteAsync(string operation, string detail, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
