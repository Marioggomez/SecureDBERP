namespace SecureERP.Infrastructure.Logging;

public interface IApplicationLogger<TCategoryName>
{
    void LogInformation(string message, params object[] args);

    void LogWarning(string message, params object[] args);

    void LogError(Exception exception, string message, params object[] args);
}
