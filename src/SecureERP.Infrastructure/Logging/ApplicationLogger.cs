using Microsoft.Extensions.Logging;

namespace SecureERP.Infrastructure.Logging;

public sealed class ApplicationLogger<TCategoryName> : IApplicationLogger<TCategoryName>
{
    private readonly ILogger<TCategoryName> _logger;

    public ApplicationLogger(ILogger<TCategoryName> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }
}
