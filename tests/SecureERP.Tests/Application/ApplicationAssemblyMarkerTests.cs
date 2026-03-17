using SecureERP.Application;

namespace SecureERP.Tests.Application;

public sealed class ApplicationAssemblyMarkerTests
{
    [Fact]
    public void MarkerType_ShouldExist_InApplicationAssembly()
    {
        Assert.Equal("SecureERP.Application", typeof(ApplicationAssemblyMarker).Assembly.GetName().Name);
    }
}
