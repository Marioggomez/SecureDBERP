namespace SecureERP.Application.Abstractions.Context;

public interface IRequestContextAccessor
{
    RequestContext Current { get; }

    void SetCurrent(RequestContext context);
}
