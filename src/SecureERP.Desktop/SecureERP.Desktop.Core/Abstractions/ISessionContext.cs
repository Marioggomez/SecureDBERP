using SecureERP.Desktop.Core.Models;

namespace SecureERP.Desktop.Core.Abstractions;

public interface ISessionContext
{
    bool IsAuthenticated { get; }

    SessionInfo? Current { get; }

    void SetSession(SessionInfo session);

    void Clear();
}