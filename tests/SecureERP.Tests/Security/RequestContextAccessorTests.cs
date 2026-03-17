using SecureERP.Application.Abstractions.Context;
using SecureERP.Infrastructure.Persistence.SessionContext;

namespace SecureERP.Tests.Security;

public sealed class RequestContextAccessorTests
{
    [Fact]
    public void SetCurrent_ShouldPersistContext()
    {
        RequestContextAccessor accessor = new();
        RequestContext context = new(1, 2, 3, Guid.NewGuid(), "corr-123");

        accessor.SetCurrent(context);

        Assert.Equal(context, accessor.Current);
    }
}
