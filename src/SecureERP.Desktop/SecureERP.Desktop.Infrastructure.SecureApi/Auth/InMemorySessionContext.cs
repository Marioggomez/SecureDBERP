using SecureERP.Desktop.Core.Abstractions;
using SecureERP.Desktop.Core.Models;

namespace SecureERP.Desktop.Infrastructure.SecureApi.Auth;

public sealed class InMemorySessionContext : ISessionContext
{
    public bool IsAuthenticated => Current is not null;

    public SessionInfo? Current { get; private set; }

    public void SetSession(SessionInfo session)
    {
        Current = session;
    }

    public void Clear()
    {
        Current = null;
    }
}