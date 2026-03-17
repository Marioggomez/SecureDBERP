namespace SecureERP.Infrastructure.Auditing;

public interface IAuditTrailWriter
{
    Task WriteAsync(string operation, string detail, CancellationToken cancellationToken = default);
}
