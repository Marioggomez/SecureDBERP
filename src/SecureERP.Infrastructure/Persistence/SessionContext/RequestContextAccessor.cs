using SecureERP.Application.Abstractions.Context;

namespace SecureERP.Infrastructure.Persistence.SessionContext;

public sealed class RequestContextAccessor : IRequestContextAccessor
{
    private RequestContext _current = RequestContext.Empty;

    public RequestContext Current => _current;

    public void SetCurrent(RequestContext context)
    {
        _current = context;
    }
}
