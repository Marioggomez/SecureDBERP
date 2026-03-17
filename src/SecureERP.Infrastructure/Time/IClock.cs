namespace SecureERP.Infrastructure.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
